
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
7.Deploy your gmsa application.(refer to the samples folder)
```
kubo@jumper:~/gmsa$ ./deploy-gmsa-webhook.sh  --file ~/gmsa/gmsa-webhook.yml 
WARNING: Certs dir gmsa-webhook-certs already exists
WARNING: gmsa-webhook-certs/server-key.pem already exists, not re-generating
WARNING: gmsa-webhook-certs/csr.conf already exists, not re-generating
WARNING: gmsa-webhook-certs/server.csr already exists, not re-generating
certificatesigningrequest.certificates.k8s.io "gmsa-webhook.gmsa-webhook" deleted
certificatesigningrequest.certificates.k8s.io/gmsa-webhook.gmsa-webhook created
NAME                        AGE   SIGNERNAME                      REQUESTOR    REQUESTEDDURATION   CONDITION
gmsa-webhook.gmsa-webhook   0s    kubernetes.io/kubelet-serving   oidc:alana   <none>              Pending
certificatesigningrequest.certificates.k8s.io/gmsa-webhook.gmsa-webhook approved
WARNING: gmsa-webhook-certs/server-cert.pem already exists, not re-generating
customresourcedefinition.apiextensions.k8s.io "gmsacredentialspecs.windows.k8s.io" deleted
customresourcedefinition.apiextensions.k8s.io/gmsacredentialspecs.windows.k8s.io created
*** using config file authentication ***
using local envsubst
namespace/gmsa-webhook created
secret/gmsa-webhook created
serviceaccount/gmsa-webhook created
clusterrole.rbac.authorization.k8s.io/gmsa-webhook-gmsa-webhook-rbac-role created
clusterrolebinding.rbac.authorization.k8s.io/gmsa-webhook-gmsa-webhook-binding-to-gmsa-webhook-gmsa-webhook-rbac-role created
deployment.apps/gmsa-webhook created
service/gmsa-webhook created
validatingwebhookconfiguration.admissionregistration.k8s.io/gmsa-webhook created
mutatingwebhookconfiguration.admissionregistration.k8s.io/gmsa-webhook created

*** Windows GMSA Admission Webhook successfully deployed! ***
*** You can remove it by running /usr/local/bin/kubectl delete -f /home/kubo/gmsa/gmsa-webhook.yml ***
kubo@jumper:~/gmsa$ kubectl apply -f gmsacrd.yaml
kubo@jumper:~/gmsa$ kubectl apply -f gmsaspec.yaml
kubo@jumper:~/gmsa$ kubectl apply -f gmsarbac.yaml
kubo@jumper:~/gmsa$ kubectl apply -f deployment.yaml

```
8.Verify your gmsa application works as expected
```
kubectl -exec -it pod -- PowerShell

PS C:\> ping gmsaping.com

Pinging gmsaping.com [10.199.17.13] with 32 bytes of data:
Reply from 10.199.17.13: bytes=32 time=2ms TTL=117
Reply from 10.199.17.13: bytes=32 time=2ms TTL=117
Reply from 10.199.17.13: bytes=32 time=2ms TTL=117

Ping statistics for 10.199.17.13:
    Packets: Sent = 3, Received = 3, Lost = 0 (0% loss),
Approximate round trip times in milli-seconds:
    Minimum = 2ms, Maximum = 2ms, Average = 2ms

PS C:\>Nltest /sc_verify:gmsaping.com
Flags: b0 HAS_IP  HAS_TIMESERV
Trusted DC Name \\WIN-5DF7THCD7UL.gmsaping.com
Trusted DC Connection Status Status = 0 0x0 NERR_Success
Trust Verification Status = 0 0x0 NERR_Success
The command completed successfully


PS C:\> klist get krbtgt

Current LogonId is 0:0x108b2bdb
A ticket to krbtgt has been retrieved successfully.

Cached Tickets: (2)

#0>     Client: WebApp01$ @ GMSAPING.COM
        Server: krbtgt/GMSAPING.COM @ GMSAPING.COM
        KerbTicket Encryption Type: AES-256-CTS-HMAC-SHA1-96
        Ticket Flags 0x40a10000 -> forwardable renewable pre_authent name_canonicalize
        Start Time: 5/21/2024 2:46:58 (local)
        End Time:   5/21/2024 12:46:58 (local)
        Renew Time: 5/28/2024 2:46:58 (local)
        Session Key Type: AES-256-CTS-HMAC-SHA1-96
        Cache Flags: 0
        Kdc Called: WIN-5DF7THCD7UL.gmsaping.com

#1>     Client: WebApp01$ @ GMSAPING.COM
        Server: krbtgt/GMSAPING.COM @ GMSAPING.COM
        KerbTicket Encryption Type: AES-256-CTS-HMAC-SHA1-96
        Ticket Flags 0x40e10000 -> forwardable renewable initial pre_authent name_canonicalize
        Start Time: 5/21/2024 2:46:58 (local)
        End Time:   5/21/2024 12:46:58 (local)
        Renew Time: 5/28/2024 2:46:58 (local)
        Session Key Type: AES-256-CTS-HMAC-SHA1-96
        Cache Flags: 0x1 -> PRIMARY
        Kdc Called: WIN-5DF7THCD7UL.gmsaping.com


PS C:\> Nltest /query
Flags: 0
Connection Status = 0 0x0 NERR_Success
The command completed successfully

```






