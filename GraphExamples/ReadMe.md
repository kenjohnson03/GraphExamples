# Overview - UpdateNewDevices
This function app

# Install - UpdateNewDevices

## Environment Variables 
AUUpdates - where the device names are mapped to the AU Ids. DeviceName is used to match the device name in Intune. This uses a contains method in the background so partial matching works where naming standards are used. AUId is the Id of the Administrative Unit in Intune. Multiple mappings can be added by separating them with a semicolon. Example: DeviceName=AUId;DeviceName=AUId;
- Format: DeviceName=AUId;DeviceName=AUId;

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

## Service Principal Permissions
The service principal used by the application must have the following Graph API permissions at a minimum in addition to any actions to take:

<b>
Directory.Read.All<br/>
DeviceManagementManagedDevices.Read.All<br/>
</b>
<br/>

The service principal used by the application must have the following Graph API permissions to update device attributes:

<b>
Device.ReadWrite.All<br/>
Directory.Read.All<br/>
DeviceManagementManagedDevices.Read.All<br/>
</b>
<br/>

For updates to Administrative Units the service principal must have the following role assignments:

<b>
Privileged Role Administrator<br/>
</b>

To update Security Groups the service principal must have the following role assignments:

Be the owner of the group.