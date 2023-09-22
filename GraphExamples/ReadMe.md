# Overview - UpdateNewDevices
This function app

# Install - UpdateNewDevices

## Environment Variables 
AUUpdates - where the device names are mapped to the AU Ids. DeviceName is used to match the device name in Intune. This uses a contains method in the background so partial matching works where naming standards are used. AUId is the Id of the Administrative Unit in Intune. Multiple mappings can be added by separating them with a semicolon. Example: DeviceName=AUId;DeviceName=AUId;
- Format: DeviceName=AUId;DeviceName=AUId;

AzureEnvironment
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

HoursToQuery
- Number of hours to query for new devices. Default is -2 which will query for devices created in the last 2 hours. This is used to prevent the function from querying for all devices in the tenant.

Portal advanced edit
```
{
	"name": "AUUpdates",
	"value": "DeviceName=AUId;DeviceName=AUId;",
	"slotSetting": false
},
{
	"name": "AzureEnvironment",
	"value": "AzurePublicCloud",
	"slotSetting": false
},
{
	"name": "AzureApp:ClientSecret",
	"value": "{Secret Here}",
	"slotSetting": false
},
{
	"name": "AzureAd:ClientId",
	"value": "{Guid Here}",
	"slotSetting": false
},
{
	"name": "AzureAd:TenantId",
	"value": "{Guid Here}",
	"slotSetting": false
},
{
	"name": "HoursToQuery",
	"value": "-2",
	"slotSetting": false
}
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