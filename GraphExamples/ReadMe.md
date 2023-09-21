# Install

## Environment Variables 
TargetCloud
- AzurePublicCloud
- AzureUSGovernment
- AzureUSDoD

AzureApp:ClientSecret
- Secret from Azure App Registration
- https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#get-values-for-signing-in
- https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#option-2-create-a-new-application-secret


AzureAd:ClientId
- Guid from Azure App Registration
- https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#get-values-for-signing-in

AzureAd:TenantId
- Guid from Azure App Registration
- https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#get-values-for-signing-in

```
TargetCloud: AzurePublicCloud
AzureApp:ClientSecret: {Secret Here}
AzureAd:ClientId: {Guid Here}
AzureAd:TenantId: {Guid Here}
```