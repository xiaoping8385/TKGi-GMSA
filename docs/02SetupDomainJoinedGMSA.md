
1.Configure TKGi, add the windows server as Node DNS from the network configuration, apply the change and upgrade the cluster.
Another simple way is to edit the /etc/resolv.conf, to add the nameserver info.
Eg.
```
kubo@jumper:~$ cat /etc/resolv.conf 
nameserver 10.xx.xx.xx
```

2.Add your container host to the domain:
   From your container host, run the following commands:
   ```
   PS C:\Users\bosh_58c8de1962b24e3> netdom query DC /domain "gmsaping.com" /userd "xxx" /passwordd "xxx"
   List of domain controllers with accounts in the domain:

   WIN-5DF7THCD7UL
   The command completed successfully.

   PS C:\Users\bosh_58c8de1962b24e3> $domainUserPasswordObject = ConvertTo-SecureString -String "xxx" -AsPlainText -Force

   PS C:\Users\bosh_58c8de1962b24e3> $domainUserCredObject = New-Object System.Management.Automation.PSCredential ("xxx@xxx.com", $domainUserPasswordObject)

   PS C:\Users\bosh_58c8de1962b24e3> Add-Computer -DomainName "gmsaping.com" -Credential $domainUserCredObject -Force
   WARNING: The changes will take effect after you restart the computer 92b9d147-fe8c-40e1-ba3f-c40d336505cd.
```
3.To verify if your host is been added, simply run:
```
   PS C:\Users\bosh_58c8de1962b24e3> Get-WmiObject -Class Win32_ComputerSystem

Domain              : gmsaping.com
Manufacturer        : VMware, Inc.
Model               : VMware Virtual Platform
Name                : WIN-8E8KH04F8SV
PrimaryOwnerName    :
TotalPhysicalMemory : 4294430720

PS C:\Users\bosh_58c8de1962b24e3> Get-ADComputer "WIN-8E8KH04F8SV" -Credential $domainUserCredObject

DistinguishedName : CN=WIN-8E8KH04F8SV,CN=Computers,DC=gmsaping,DC=com
DNSHostName       : 92b9d147-fe8c-40e1-ba3f-c40d336505cd.gmsaping.com
Enabled           : True
Name              : WIN-8E8KH04F8SV
ObjectClass       : computer
ObjectGUID        : d5d7d853-2699-4caf-a7aa-58a6ecdf4d79
SamAccountName    : WIN-8E8KH04F8SV$
SID               : S-1-5-21-922537405-2102905723-2121843970-1108
UserPrincipalName :
```
4.If you want to check the domain information, simply run below 
```
PS C:\Users\bosh_58c8de1962b24e3> $ADDomain = Get-ADDomain -Server "gmsaping.com" -Credential $domainUserCredObject
PS C:\Users\bosh_58c8de1962b24e3> echo $ADDomain


AllowedDNSSuffixes                 : {}
ChildDomains                       : {}
ComputersContainer                 : CN=Computers,DC=gmsaping,DC=com
DeletedObjectsContainer            : CN=Deleted Objects,DC=gmsaping,DC=com
DistinguishedName                  : DC=gmsaping,DC=com
DNSRoot                            : gmsaping.com
DomainControllersContainer         : OU=Domain Controllers,DC=gmsaping,DC=com
DomainMode                         : Windows2012R2Domain
DomainSID                          : S-1-5-21-922537405-2102905723-2121843970
ForeignSecurityPrincipalsContainer : CN=ForeignSecurityPrincipals,DC=gmsaping,DC=com
Forest                             : gmsaping.com
InfrastructureMaster               : WIN-5DF7THCD7UL.gmsaping.com
LastLogonReplicationInterval       :
LinkedGroupPolicyObjects           : {CN={31B2F340-016D-11D2-945F-00C04FB984F9},CN=Policies,CN=System,DC=gmsaping,DC=com}
LostAndFoundContainer              : CN=LostAndFound,DC=gmsaping,DC=com
ManagedBy                          :
Name                               : gmsaping
NetBIOSName                        : GMSAPING
ObjectClass                        : domainDNS
ObjectGUID                         : bae5a583-612f-4e8a-9b69-9df1c5c9b81a
ParentDomain                       :
PDCEmulator                        : WIN-5DF7THCD7UL.gmsaping.com
PublicKeyRequiredPasswordRolling   :
QuotasContainer                    : CN=NTDS Quotas,DC=gmsaping,DC=com
ReadOnlyReplicaDirectoryServers    : {}
ReplicaDirectoryServers            : {WIN-5DF7THCD7UL.gmsaping.com}
RIDMaster                          : WIN-5DF7THCD7UL.gmsaping.com
SubordinateReferences              : {DC=DomainDnsZones,DC=gmsaping,DC=com, DC=ForestDnsZones,DC=gmsaping,DC=com,
                                     CN=Configuration,DC=gmsaping,DC=com}
SystemsContainer                   : CN=System,DC=gmsaping,DC=com
UsersContainer                     : CN=Users,DC=gmsaping,DC=com
```
5.Create a service account,run below command in Domain controller(which holds the AD server)
```
Add-KdsRootKey -EffectiveTime ((get-date).addhours(-10))
# Replace 'WebApp01' and 'contoso.com' with your own gMSA and domain names, respectively.
# To install the AD module on Windows Server, run Install-WindowsFeature RSAT-AD-PowerShell
# To install the AD module on Windows 10 version 1809 or later, run Add-WindowsCapability -Online -Name 'Rsat.ActiveDirectory.DS-LDS.Tools~~~~0.0.1.0'
# To install the AD module on older versions of Windows 10, see https://aka.ms/rsat

# Create the security group
New-ADGroup -Name "WebApp01 Authorized Hosts" -SamAccountName "WebApp01Hosts" -GroupScope DomainLocal

# Create the gMSA
New-ADServiceAccount -Name "WebApp01" -DnsHostName "WebApp01.gmsaping.com" -ServicePrincipalNames "host/WebApp01", "host/WebApp01.gmsaping.com" -PrincipalsAllowedToRetrieveManagedPassword "WebApp01Hosts"
```

6. Add your container hosts to the security group, execute in your container host
```

PS C:\Users\bosh_7ff2b21529fb42d> $computer=Get-ADComputer "WIN-8E8KH04F8SV" -Credential $domainUserCredObject
PS C:\Users\bosh_7ff2b21529fb42d> Add-ADGroupMember -Identity "WebApp01Hosts" -Members $computer -Credential $domainUserCredObject
PS C:\Users\bosh_7ff2b21529fb42d> Get-ADGroupMember -Identity WebApp01Hosts -Credential $domainUserCredObject

distinguishedName : CN=WIN-8E8KH04F8SV,CN=Computers,DC=gmsaping,DC=com
name              : WIN-8E8KH04F8SV
objectClass       : computer
objectGUID        : d5d7d853-2699-4caf-a7aa-58a6ecdf4d79
SamAccountName    : WIN-8E8KH04F8SV$
SID               : S-1-5-21-922537405-2102905723-2121843970-1108
```



