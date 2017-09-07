using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using LPP.Model;
using LPP.Models;
using System.Net.Mail;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;

namespace LPP
{
    public class EmailService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your email service here to send an email.
            MailMessage mail = new MailMessage();

            // The important part -- configuring the SMTP client
            SmtpClient smtp = new SmtpClient();
            //recipient address
            mail.To.Add(new MailAddress(message.Destination));

            //Formatted mail body
            mail.IsBodyHtml = true;
            mail.Body = message.Body;
            mail.Subject = message.Subject;
            try
            {
                smtp.Send(mail);
            }
            catch (Exception)
            {
                throw;
            }
            return Task.FromResult(0);
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }

    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        private readonly PrincipalContext _context;

        public ApplicationUserManager(IUserStore<ApplicationUser> store, PrincipalContext context)
            : base(store)
        {
            this._context = context;
        }


        public async Task<ADSignInStatus> ADLoginAsync(string username, string password)
        {
            PrincipalContext _adContext = new PrincipalContext(ContextType.Domain, Properties.Settings.Default.ADPrincipalName, Properties.Settings.Default.ADContainer, ContextOptions.SimpleBind, Properties.Settings.Default.ADUserName, Properties.Settings.Default.ADPassword);

            bool _status = _adContext.ValidateCredentials(username, password, ContextOptions.Negotiate);
            if (_status)
            {
                return await Task.FromResult(ADSignInStatus.Success);
            }
            else
            {
                var _adUser = await Task.FromResult<UserPrincipal>(UserPrincipal.FindByIdentity(_adContext, IdentityType.SamAccountName, username));
                if (_adUser == null)
                {
                    var _lppUser = await Task.FromResult<UserPrincipal>(UserPrincipal.FindByIdentity(_context, IdentityType.SamAccountName, username));
                    if (_lppUser != null)
                    {
                        return await Task.FromResult(ADSignInStatus.AccountRemoved);
                    }
                    else
                    {
                        return await Task.FromResult(ADSignInStatus.Failure);
                    }
                }
                else
                {
                    if (GetPasswordExpirationDays(username, Properties.Settings.Default.ADUserName, Properties.Settings.Default.ADPassword) < 0)
                    {
                        return await Task.FromResult(ADSignInStatus.PasswordExpired);
                    }
                    else
                    {
                        return await Task.FromResult(ADSignInStatus.Failure);
                    }
                }
            }

        }

        public long GetPasswordExpirationDays(string username, string ADUserName, string ADPassWord)
        {
            DirectoryEntry rootDSE = null;
            DirectoryEntry searchRoot = null;
            DirectoryEntry userEntry = null;
            DirectorySearcher searcher = null;
            SearchResultCollection results = null;
            long daysLeft = 0;


            try
            {
                // Look up user in AD
                string ADDomain = Properties.Settings.Default.ADDomain;
                string ADconnectionString = Properties.Settings.Default.ADconnectionString;

                //get maximum password age for PF AD
                DirectoryEntry entry = new DirectoryEntry(ADDomain, ADUserName, ADPassWord, AuthenticationTypes.Secure);

                DirectorySearcher mySearcher = new DirectorySearcher(entry);

                string filter = "maxPwdAge=*";
                mySearcher.Filter = filter;

                results = mySearcher.FindAll();
                long maxDays = 0;
                if (results.Count >= 1)
                {
                    Int64 maxPwdAge = (Int64)results[0].Properties["maxPwdAge"][0];
                    maxDays = maxPwdAge / -864000000000L;
                }

                //get days left to expiare password with last password set date
                userEntry = new DirectoryEntry(ADconnectionString, ADUserName, ADPassWord, AuthenticationTypes.Secure);

                // Was user's AD entry found?
                if (userEntry == null)
                {
                    Logs.Info(string.Format("user {0} not found in PF AD.", username));
                }

                searcher = new DirectorySearcher(userEntry);
                searcher.Filter = string.Format("sAMAccountName={0}", username);
                searcher.SearchScope = SearchScope.Subtree;
                searcher.CacheResults = false;
                results = searcher.FindAll();

                if (results.Count >= 1)
                {
                    object lastChanged = results[0].Properties["pwdLastSet"][0];
                    daysLeft = maxDays - DateTime.Today.Subtract(DateTime.FromFileTime(Convert.ToInt64(lastChanged))).Days;
                }

            }
            catch (Exception ex)
            {
                Logs.Error(ex);

            }
            finally
            {
                if ((userEntry != null))
                    userEntry.Dispose();
                if ((results != null))
                    results.Dispose();
                if ((searcher != null))
                    searcher.Dispose();
                if ((searchRoot != null))
                    searchRoot.Dispose();
                if ((rootDSE != null))
                    rootDSE.Dispose();
            }
            return daysLeft;
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
        {
            //var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(), context.Get<PrincipalContext>());
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 0,
                //RequireNonLetterOrDigit = true,
                //RequireDigit = true,
                //RequireLowercase = true,
                //RequireUppercase = true,
            };

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug it in here.
            manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<ApplicationUser>
            {
                MessageFormat = "Your security code is {0}"
            });
            manager.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<ApplicationUser>
            {
                Subject = "Security Code",
                BodyFormat = "Your security code is {0}"
            });
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider =
                    new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }

    // Configure the application sign-in manager which is used in this application.
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager, DefaultAuthenticationTypes.ApplicationCookie);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }
    //custom class to get or set Identity object properties
    public class AppUserPrincipal : ClaimsPrincipal
    {
        public AppUserPrincipal(ClaimsPrincipal principal)
            : base(principal)
        {
        }

        /// <summary>
        /// Current user name
        /// </summary>
        public string UserName
        {
            get
            {
                return this.FindFirst(ClaimTypes.Name).Value;
            }
        }

        /// <summary>
        /// Current user password
        /// </summary>
        public string UserPassword
        {
            get
            {
                return this.FindFirst(ClaimTypes.Dsa).Value;
            }
            set
            {
                var identity = (HttpContext.Current.User as ClaimsPrincipal).Identity as ClaimsIdentity;
                identity.RemoveClaim(this.FindFirst(ClaimTypes.Dsa));

                var AuthenticationManager = HttpContext.Current.GetOwinContext().Authentication;
                identity.AddClaim(new Claim(ClaimTypes.Dsa, value));
                AuthenticationManager.AuthenticationResponseGrant = new AuthenticationResponseGrant(
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true }
                );
            }
        }

        /// <summary>
        /// Get or set current user full name
        /// </summary>
        public string UserDisplayName
        {
            get
            {
                return this.FindFirst(ClaimTypes.GivenName).Value;
            }
            set
            {
                var identity = (HttpContext.Current.User as ClaimsPrincipal).Identity as ClaimsIdentity;
                identity.RemoveClaim(this.FindFirst(ClaimTypes.GivenName));

                var AuthenticationManager = HttpContext.Current.GetOwinContext().Authentication;
                identity.AddClaim(new Claim(ClaimTypes.GivenName, value));
                AuthenticationManager.AuthenticationResponseGrant = new AuthenticationResponseGrant(
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true }
                );
            }
        }

        /// <summary>
        /// Get or set current user email
        /// </summary>
        public string UserEmail
        {
            get
            {
                return this.FindFirst(ClaimTypes.Email).Value;
            }
            set
            {
                var identity = (HttpContext.Current.User as ClaimsPrincipal).Identity as ClaimsIdentity;
                identity.RemoveClaim(this.FindFirst(ClaimTypes.Email));

                var AuthenticationManager = HttpContext.Current.GetOwinContext().Authentication;
                identity.AddClaim(new Claim(ClaimTypes.Email, value));
                AuthenticationManager.AuthenticationResponseGrant = new AuthenticationResponseGrant(
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true }
                );
            }
        }

        /// <summary>
        /// Get or set current user Top level
        /// </summary>
        public string UserTopLevel
        {
            get
            {
                return this.FindFirst(ClaimTypes.Role).Value;
            }
            set
            {
                var identity = (HttpContext.Current.User as ClaimsPrincipal).Identity as ClaimsIdentity;
                identity.RemoveClaim(this.FindFirst(ClaimTypes.Role));

                var AuthenticationManager = HttpContext.Current.GetOwinContext().Authentication;
                identity.AddClaim(new Claim(ClaimTypes.Role, value));
                AuthenticationManager.AuthenticationResponseGrant = new AuthenticationResponseGrant(
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true }
                );
            }
        }

        /// <summary>
        /// Get or set current User Paging
        /// </summary>
        public string UserPaging
        {
            get
            {
                return this.FindFirst(ClaimTypes.Version).Value;
            }
            set
            {
                var identity = (HttpContext.Current.User as ClaimsPrincipal).Identity as ClaimsIdentity;
                identity.RemoveClaim(this.FindFirst(ClaimTypes.Version));

                var AuthenticationManager = HttpContext.Current.GetOwinContext().Authentication;
                identity.AddClaim(new Claim(ClaimTypes.Version, value));
                AuthenticationManager.AuthenticationResponseGrant = new AuthenticationResponseGrant(
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true }
                );
            }
        }

        /// <summary>
        /// Get or set current user departments
        /// </summary>
        public string UserDepartment
        {
            get
            {
                return this.FindFirst(ClaimTypes.Dns).Value;
            }
            set
            {
                var identity = (HttpContext.Current.User as ClaimsPrincipal).Identity as ClaimsIdentity;
                identity.RemoveClaim(this.FindFirst(ClaimTypes.Dns));

                var AuthenticationManager = HttpContext.Current.GetOwinContext().Authentication;
                identity.AddClaim(new Claim(ClaimTypes.Dns, value));
                AuthenticationManager.AuthenticationResponseGrant = new AuthenticationResponseGrant(
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true }
                );
            }
        }

        /// <summary>
        /// Get or set current user locations
        /// </summary>
        public string UserLocation
        {
            get
            {
                return this.FindFirst(ClaimTypes.PrimarySid).Value;
            }
            set
            {
                var identity = (HttpContext.Current.User as ClaimsPrincipal).Identity as ClaimsIdentity;
                identity.RemoveClaim(this.FindFirst(ClaimTypes.PrimarySid));

                var AuthenticationManager = HttpContext.Current.GetOwinContext().Authentication;
                identity.AddClaim(new Claim(ClaimTypes.PrimarySid, value));
                AuthenticationManager.AuthenticationResponseGrant = new AuthenticationResponseGrant(
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true }
                );
            }
        }

        /// <summary>
        /// Get or set current user organizations
        /// </summary>
        public string UserOrganization
        {
            get
            {
                return this.FindFirst(ClaimTypes.PrimaryGroupSid).Value;
            }
            set
            {
                var identity = (HttpContext.Current.User as ClaimsPrincipal).Identity as ClaimsIdentity;
                identity.RemoveClaim(this.FindFirst(ClaimTypes.PrimaryGroupSid));

                var AuthenticationManager = HttpContext.Current.GetOwinContext().Authentication;
                identity.AddClaim(new Claim(ClaimTypes.PrimaryGroupSid, value));
                AuthenticationManager.AuthenticationResponseGrant = new AuthenticationResponseGrant(
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true }
                );
            }
        }

        /// <summary>
        /// get or set current user options
        /// </summary>
        public string UserOptions
        {
            get
            {
                return this.FindFirst(ClaimTypes.Upn).Value;
            }
            set
            {
                var identity = (HttpContext.Current.User as ClaimsPrincipal).Identity as ClaimsIdentity;
                identity.RemoveClaim(this.FindFirst(ClaimTypes.Upn));

                var AuthenticationManager = HttpContext.Current.GetOwinContext().Authentication;
                identity.AddClaim(new Claim(ClaimTypes.Upn, value));
                AuthenticationManager.AuthenticationResponseGrant = new AuthenticationResponseGrant(
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true }
                );
            }
        }

        /// <summary>
        /// Get or set current user prefix
        /// </summary>
        public string Prefix
        {
            get
            {
                return this.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
            set
            {
                var identity = (HttpContext.Current.User as ClaimsPrincipal).Identity as ClaimsIdentity;
                identity.RemoveClaim(this.FindFirst(ClaimTypes.NameIdentifier));

                var AuthenticationManager = HttpContext.Current.GetOwinContext().Authentication;
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, value));
                AuthenticationManager.AuthenticationResponseGrant = new AuthenticationResponseGrant(
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true }
                );
            }
        }
    }
}
