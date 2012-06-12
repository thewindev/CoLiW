﻿using System;
using System.Dynamic;
using System.Security.Authentication;
using System.Windows.Forms;
using Facebook;

namespace CoLiW
{
    public class Facebook : FacebookClient
    {
        public FacebookLoginForm FbLoginForm;

        public Uri LogoutUri { get; set; }

        public Facebook()
        {
            
        }

        public Facebook(string appId, string appSecret)
        {
            AppId = appId;
            AppSecret = appSecret;
            FbLoginForm = new FacebookLoginForm();
            FbLoginForm.Browser.Navigated += BrowserNavigated;
            if (IsLoggedIn() && string.IsNullOrEmpty(AccessToken) == true)
                Login(false);

        }

        public bool IsLoggedIn()
        {
            try
            {
                FbLoginForm.IsLoggedIn = true; //assume that the user is logged in
                UserDetails me = GetUserDetails("me"); //try to get his name
                return true;
            }
            catch (Exception exception) //if there is an exception, then the user is logged out
            {
                if (exception is FacebookOAuthException && exception.Message.Contains("#2500")) //especially if the exception is an oauthexception
                {
                    return true;
                }
                else return false; //but if there is another exception, then login
            }
        }

        public bool Login(bool forceLogin)
        {
            if (FbLoginForm.IsLoggedIn && forceLogin == false)
                //if the user is logged in and force login = false, then exit
                return true;
            if (forceLogin && FbLoginForm.IsLoggedIn)
                //if the user is logged in, and forcelogin = true, then logout first
                Logout();
            FbLoginForm.Browser.Navigate(GetLoginUri("publish_stream"));
            if (FbLoginForm.ShowDialog() == DialogResult.No)
                return false;
            return true;
        }

        public bool Logout()
        {
            LogoutUri =
                    GetLogoutUrl(
                        new
                        {
                            access_token = AccessToken,
                            next = "https://www.facebook.com/connect/login_success.html"
                        });
            FbLoginForm.Browser.Navigate(LogoutUri);
            FbLoginForm.ShowDialog();
            FbLoginForm.IsLoggedIn = false;
            return false;
        }

        public Uri GetLoginUri(string extendedPermissions)
        {
            try
            {
                dynamic parameters = new ExpandoObject();
                parameters.client_id = AppId;
                parameters.redirect_uri = "https://www.facebook.com/connect/login_success.html";

                // The requested response: an access token (token), an authorization code (code), or both (code token).
                parameters.response_type = "token";

                // list of additional display modes can be found at http://developers.facebook.com/docs/reference/dialogs/#display
                parameters.display = "popup";

                // add the 'scope' parameter only if we have extendedPermissions.
                if (!string.IsNullOrWhiteSpace(extendedPermissions))
                    parameters.scope = extendedPermissions;

                // generate the login url

                Uri loginUri = GetLoginUrl(parameters);
                return loginUri;
            }
            catch (Exception exception)
            {
                return new Uri(exception.ToString());
            }
        }


        public bool Login(string appId, string appSecret, bool forceLogin)
        {
            try
            {
                ValidateLoginCredentials(appId, appSecret);

                AppId = appId;
                AppSecret = appSecret;

                Login(forceLogin);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool ValidateLoginCredentials(string appId, string appSecret)
        {
            try
            {
                if (appId.Length != 15)
                    throw new InvalidCredentialException("The application id doesn't have a length of 15 characters");
                for (int i = 'A'; i < 'z'; i++)
                {
                    string iChar = "";
                    iChar += (char) i;
                    if (appId.Contains(iChar))
                    {
                        throw new InvalidCredentialException("The application id contains invalid characters");
                    }
                }
                if (appSecret.Length != 32)
                    throw new InvalidCredentialException("The application secret doesn't have a length of 32 characters");
                return true;
            }
            catch (Exception exception)
            {
                if (exception is InvalidCredentialException)
                    return false;
                throw new Exception("Something went wrong when validating your credentials");
            }
        }

        public bool AddPhoto(PhotoDetails photoDetails)
        {
            return false;
        }

        public UserDetails GetUserDetails(string userName)
        {
            if(FbLoginForm.IsLoggedIn == false)
                throw new FacebookOAuthException("The user is not logged in");

            dynamic user = Get(userName);
            var userDetails = new UserDetails();
            if(user.name != null)
                userDetails.Name = (string) user.name;
            if (user.first_name != null)
                userDetails.FirstName = (string) user.first_name;
            if (user.last_name != null)
                userDetails.LastName = (string) user.last_name;
            if (user.link != null)
                userDetails.ProfileUrl = (string) user.link;
            if (user.username != null)
                userDetails.Username = (string) user.username;
            if (user.gender != null)
                userDetails.Gender = (string) user.gender;
            if (user.id != null)
                userDetails.Id = (string) user.id;
            return userDetails;
        }

        public bool CreateAlbum(AlbumDetails albumDetails)
        {
            return false;
        }

        private void BrowserNavigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if(FbLoginForm.IsLoggedIn == false)
            {
                FacebookOAuthResult result;
                if(TryParseOAuthCallbackUrl(e.Url, out result))
                {
                    AccessToken = result.AccessToken;
                    FbLoginForm.DialogResult = DialogResult.OK;
                    FbLoginForm.IsLoggedIn = true;
                    FbLoginForm.Close();
                }
            }
            else
            {
                FbLoginForm.IsLoggedIn = false;
                FbLoginForm.Close();
            }
        }
    }
}