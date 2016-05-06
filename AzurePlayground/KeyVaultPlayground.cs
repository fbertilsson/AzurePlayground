using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AzurePlayground
{
    public class KeyVaultPlayground
    {
        private Guid _clientId;
        private Guid _deploymentAppClientId;
        private Guid _tenantId;
        private UserCredential _userCredential;
        private string _resourceGroupName;
        private string _vaultName;
        private AuthenticationResult _authenticationResult;


        public KeyVaultPlayground()
        {
            _deploymentAppClientId = new Guid("4c28008e-b19b-4f05-93d2-56cc5194c294");
            _clientId = _deploymentAppClientId;
            _tenantId = new Guid("059c5599-663c-491c-b6ea-844c241fc531");
            _userCredential = new UserCredential("admin@sb1vptest.onmicrosoft.com", "");
            _vaultName = "vp-vault-test";
            _resourceGroupName = "vp-resgroup-test";

            //var permissionJson = @"{ ""keys"": [ ""all"" ], ""secrets"": [ ""all"" ] }";

            //var vaultConfig = new KeyVaultConfig(
            //    new List<AccessPolicyEntry> {
            //        new AccessPolicyEntry
            //        {
            //            ApplicationId = _deploymentAppClientId,
            //            ObjectId = Guid.Empty,                      // Replace later in this program
            //            PermissionsRawJsonString = permissionJson,
            //        },
            //        new AccessPolicyEntry
            //        {
            //            ApplicationId = _deploymentAppClientId,
            //            ObjectId = Guid.Empty,                      // Replace later in this program
            //            PermissionsRawJsonString = permissionJson,
            //        },
            //    },
            //    true, true, false,
            //    new Sku { Family = "A", Name = "Standard" },
            //    _tenantId);
        }

        public void CreateOrUpdateKeyVault()
        {
            //var cloudCredentials = ...
            //using (var client = new KeyVaultManagementClient(credentials))
            //{
            //    var properties = new VaultProperties
            //    {
            //        AccessPolicies = GetAccessPolicies(_deploymentAppClientId),
            //        EnabledForDeployment = true,
            //        EnabledForDiskEncryption = true,
            //        EnabledForTemplateDeployment = false,
            //        Sku = new Sku { Family = "A", Name = "Standard" },
            //        TenantId = _tenantId
            //    };
            //    var parameters = new VaultCreateOrUpdateParameters(properties, "West Europe");

            //    var result = await
            //        client.Vaults.CreateOrUpdateAsync(_resourceGroupName, _vaultName, parameters, cancel)
            //            .ConfigureAwait(false);

            //    // TODO check status: if (result.StatusCode)
            //}
        }

        public void CreateSecret() { 
            var vaultClient = new KeyVaultClient((authority, resource, scope) =>
            {
                //var credential = new ClientCredential(_clientId, applicationSecret);
                var authenticationContext = new AuthenticationContext(authority, null);
                _authenticationResult = authenticationContext.AcquireToken(
                    resource, 
                    _deploymentAppClientId.ToString(), 
                    _userCredential);
                return Task.FromResult(_authenticationResult.AccessToken);
            });

            var secretName = "fb-testing3";
            //var idHelper = new KeyVaultIdentifierHelper(result.Vault.Properties.VaultUri);
            //var secretUri = idHelper.GetSecretIdentifier(secretName);
            var vaultUri = "https://vp-vault-test.vault.azure.net/";
            var secretUri = $"{vaultUri}secrets/{secretName}";
            try
            {
                // Does the _application_ need to be authorized to do this? Is that why it fails with "Bad request"?
                var secret = vaultClient.SetSecretAsync(
                    vaultUri,
                    secretUri,
                    "very secret").ConfigureAwait(false);
                Console.WriteLine(secret);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private IList<AccessPolicyEntry> GetAccessPolicies(Guid deployAppClientId)
        {
            const string permissionJson = @"{ ""keys"": [ ""all"" ], ""secrets"": [ ""all"" ] }";

            var guidsForUsers = new Dictionary<string, Guid>
            {
                { "fredrik.bertilsson@sb1vptest.onmicrosoft.com", new Guid("50feb57d-6dc1-4890-bf69-cd2d311e655f")},
                { "admin@sb1vptest.onmicrosoft.com", new Guid("2e5dafc2-e3e3-49f4-ac12-451053704a9c")}
            };
            return new List<AccessPolicyEntry>
            {
                new AccessPolicyEntry
                {
                    ApplicationId = deployAppClientId,
                    ObjectId = guidsForUsers["admin@sb1vptest.onmicrosoft.com"],
                    PermissionsRawJsonString = permissionJson,
                    //PermissionsToKeys = new [] { "all" },
                    //PermissionsToSecrets = new [] { "all" },
                    TenantId = _tenantId,
                },
                new AccessPolicyEntry
                {
                    ApplicationId = deployAppClientId,
                    ObjectId = guidsForUsers["fredrik.bertilsson@sb1vptest.onmicrosoft.com"],
                    PermissionsRawJsonString = permissionJson,
                    TenantId = _tenantId,
                },
            };
        }
    }
}