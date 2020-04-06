//===============================================================================
// Microsoft FastTrack for Azure
// Azure Active Directory B2C User Management Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AADB2C.GraphService
{
    public enum AccountType
    {
        None,
        LocalOnly,
        SocialOnly,
        LocalAndSocial
    }

    public class GraphAccountModel
    {
        public string objectId { get; set; }
        public bool accountEnabled { get; set; }
        public string mailNickname { get; set; }
        public IList<SignInName> signInNames { get; set; }
        public string creationType { get; set; }
        public string displayName { get; set; }
        public string givenName { get; set; }
        public string surname { get; set; }

        //public string strongAuthenticationEmailAddress { get; set; }
        public PasswordProfile passwordProfile { get; set; }
        public string passwordPolicies { get; set; }

        // Social account properties
        public IList<UserIdentity> userIdentities { get; set; }
        public IList<string> otherMails { get; set; }
        public string userPrincipalName { get; set; }

        // Custom attributes
        public string extension_jdrfConsId { get; set; }

        public GraphAccountModel() { }
        public GraphAccountModel(
            string tenant,
            string userType,
            string signInName,
            string issuer,
            string issuerUserId,
            string email,
            string password,
            string displayName,
            string givenName,
            string surname,
            string extension_jdrfConsId)
        {
            // Set the type of the account to be ceate
            AccountType accountType = AccountType.None;

            if ((string.IsNullOrEmpty(signInName) == false) && (string.IsNullOrEmpty(issuerUserId) == false))
                accountType = AccountType.LocalAndSocial;
            else if ((string.IsNullOrEmpty(signInName) == false) && (string.IsNullOrEmpty(issuerUserId) == true))
                accountType = AccountType.LocalOnly;
            else if ((string.IsNullOrEmpty(signInName) == true) && (string.IsNullOrEmpty(issuerUserId) == false))
                accountType = AccountType.SocialOnly;

            this.accountEnabled = true;

            // For local account, always set to 'LocalAccount
            // For social account only, set to null
            if (accountType == AccountType.SocialOnly)
                this.creationType = null;
            else
                this.creationType = "LocalAccount";

            // For local account set the signInNames 
            // For social account ONLY, this attribute can be left empty
            this.signInNames = new List<SignInName>();

            if (accountType != AccountType.SocialOnly)
                this.signInNames.Add(new SignInName(userType, signInName));

            this.mailNickname = Guid.NewGuid().ToString();

            this.userPrincipalName = $"{this.mailNickname}@{tenant}";

            // For social account, you can specify the alternate email
            List<string> otherMails = new List<string>();

            if ( accountType != AccountType.LocalOnly && (!string.IsNullOrEmpty(email)))
                otherMails.Add(email);

            this.otherMails = otherMails;

            // For social account, user social identity
            this.userIdentities = new List<UserIdentity>();

            if (accountType != AccountType.LocalOnly)
                this.userIdentities.Add(new UserIdentity(issuer, Base64Encode(issuerUserId)));

            // For socical account only, required to set password. Even though you provide the password,
            // Azure AD B2C will ignore the value and set the password profile to null
            if (accountType == AccountType.SocialOnly)
                this.passwordProfile = new PasswordProfile("!Q2w3e4r");
            else
            {
                this.passwordProfile = new PasswordProfile(password);
                this.passwordPolicies = "DisablePasswordExpiration,DisableStrongPassword";
            }

            // Other user profiel attribures
            this.displayName = displayName;
            this.givenName = givenName;
            this.surname = surname;

            this.extension_jdrfConsId = extension_jdrfConsId;
            //if (userType.ToLower() == "username")
            //{
            //    this.strongAuthenticationEmailAddress = email;
            //}

        }

        /// <summary>
        /// Converts the issuerUserId to based 64
        /// </summary>
        public static string Base64Encode(string issuerUserId)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(issuerUserId);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Serialize the object into Json string
        /// </summary>
        public override string ToString()
        {
            string json = JsonConvert.SerializeObject(this);
            return json.Replace("extension_jdrfConsId", "extension_c9fd2a89542146eab0d6de579d5036f3_jdrfConsId");
            //return JsonConvert.SerializeObject(this);
        }

        public static GraphAccountModel Parse(string JSON)
        {
            string json = JSON.Replace("extension_c9fd2a89542146eab0d6de579d5036f3_jdrfConsId", "extension_jdrfConsId");
            return JsonConvert.DeserializeObject(json, typeof(GraphAccountModel)) as GraphAccountModel;
            //return JsonConvert.DeserializeObject(JSON, typeof(GraphAccountModel)) as GraphAccountModel;
        }
    }

    public class PasswordProfile
    {
        public string password { get; set; }
        public bool forceChangePasswordNextLogin { get; set; }

        public PasswordProfile(string password)
        {
            this.password = password;

            // always set to false
            this.forceChangePasswordNextLogin = false;
        }
    }
    public class SignInName
    {
        public string type { get; set; }
        public string value { get; set; }

        public SignInName(string type, string value)
        {
            // Type must be 'emailAddress' (or 'userName')
            this.type = type;

            // The user email address
            this.value = value;
        }
    }

    public class UserIdentity
    {
        public string issuer { get; set; }
        public string issuerUserId { get; set; }

        public UserIdentity(string issuer, string issuerUserId)
        {
            // The identity provider name, such as facebook.com 
            this.issuer = issuer;

            // A unique user identifier for the issuer
            this.issuerUserId = issuerUserId;
        }
    }

    public class GraphUserSetPasswordModel
    {
        public PasswordProfile passwordProfile { get; }
        public string passwordPolicies { get; }

        public GraphUserSetPasswordModel(string password)
        {
            this.passwordProfile = new PasswordProfile(password);
            this.passwordPolicies = "DisablePasswordExpiration,DisableStrongPassword";
        }
    }

    public class GraphUserUpdateModel
    {
        public string displayName { get; set; }
        public string givenName { get; set; }
        public string surname { get; set; }
        public string extension_jdrfConsId { get; set; }

        public GraphUserUpdateModel() { }

        public GraphUserUpdateModel(string displayName, string givenName, string surname, string extension_jdrfConsId)
        {
            this.displayName = displayName;
            this.givenName = givenName;
            this.surname = surname;
            this.extension_jdrfConsId = extension_jdrfConsId;
        }

        /// <summary>
        /// Serialize the object into Json string
        /// </summary>
        public override string ToString()
        {
            string json = JsonConvert.SerializeObject(this);
            return json.Replace("extension_jdrfConsId", "extension_c9fd2a89542146eab0d6de579d5036f3_jdrfConsId");
        }
    }
}
