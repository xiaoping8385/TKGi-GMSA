1. Create and install the gmsa plugin
```
1.Copy the ccgplugin.zip to your windows host, extract them in the some directory
2.Execute the install-plugin.ps which install the dll and register it as ccg plugin
```

2. In your domain Controller, create the security group, gmsa account, and a gmsa user account, add the user account to the security group
```
# Create the security group
New-ADGroup -Name "WebApp02 Authorized Accounts" -SamAccountName "WebApp02Accounts" -GroupScope DomainLocal

# Create the gMSA
New-ADServiceAccount -Name "WebApp02" -DnsHostName "WebApp02.gmsaping.com" -ServicePrincipalNames "host/WebApp02", "host/WebApp02.contoso.com" -PrincipalsAllowedToRetrieveManagedPassword "WebApp02Accounts"

# Create the standard user account. This account information needs to be stored in a secret store and will be retrieved by the ccg.exe hosted plug-in to retrieve the gMSA password. Replace 'StandardUser01' and 'p@ssw0rd' with a unique username and password. We recommend using a random, long, machine-generated password.
New-ADUser -Name "StandardUser01" -AccountPassword (ConvertTo-SecureString -AsPlainText "p@ssw0rd" -Force) -Enabled 1

# Add your container hosts to the security group
Add-ADGroupMember -Identity "WebApp02Accounts" -Members "StandardUser01"
```

3. Edit your gmsaspec.yaml, addint the HostAccountConfig to your spec
```
  apiVersion: windows.k8s.io/v1
kind: GMSACredentialSpec
metadata:
  name: gmsa-webapp02  # This is an arbitrary name but it will be used as a reference
credspec:
  ActiveDirectoryConfig:
    GroupManagedServiceAccounts:
    - Name: WebApp02   # Username of the GMSA account
      Scope: GMSAPING  # NETBIOS Domain Name
    - Name: WebApp02   # Username of the GMSA account
      Scope: gmsaping.com # DNS Domain Name
    HostAccountConfig:
      PluginGUID: "{f919de1a-efc4-4902-b7e5-56a314a87262}"
      PluginInput: DOMAINNAME=gmsaping.com;USERNAME=StandardUser01;PASSWORD=p@ssw0rd;logFile=c:\\temp\\logfile.log
      PortableCcgVersion: "1"
  CmsPlugins:
  - ActiveDirectory
  DomainJoinConfig:
    DnsName: gmsaping.com  # DNS Domain Name
    DnsTreeName: gmsaping.com # DNS Domain Name Root
    Guid: bae5a583-612f-4e8a-9b69-9df1c5c9b81a  # GUID
    MachineAccountName: WebApp02 # Username of the GMSA account
    NetBiosName: GMSAPING  # NETBIOS Domain Name
    Sid: S-1-5-21-922537405-2102905723-2121843970 # SID of GMSA
```
4.Install the rest of the resource in the sample/gmsa_non_domain_joined, including gmsa-crd.yml. gmsarbac.yaml, gmsaspec.yml,gmsa-webhook.yaml,deployment.yaml and validate your application.
```
PS C:\> Nltest /query
Flags: 0
Connection Status = 0 0x0 NERR_Success
The command completed successfully
PS C:\> Nltest /sc_verify:gmsaping.com
Flags: b0 HAS_IP  HAS_TIMESERV
Trusted DC Name \\WIN-5DF7THCD7UL.gmsaping.com
Trusted DC Connection Status Status = 0 0x0 NERR_Success
Trust Verification Status = 0 0x0 NERR_Success
The command completed successfully
PS C:\> klist get krbtgt

Current LogonId is 0:0x2f864f1
A ticket to krbtgt has been retrieved successfully.

Cached Tickets: (2)

#0>     Client: WebApp02$ @ GMSAPING.COM
        Server: krbtgt/GMSAPING.COM @ GMSAPING.COM
        KerbTicket Encryption Type: AES-256-CTS-HMAC-SHA1-96
        Ticket Flags 0x40a10000 -> forwardable renewable pre_authent name_canonicalize
        Start Time: 5/21/2024 7:20:13 (local)
        End Time:   5/21/2024 17:20:13 (local)
        Renew Time: 5/28/2024 7:20:13 (local)
        Session Key Type: AES-256-CTS-HMAC-SHA1-96
        Cache Flags: 0
        Kdc Called: WIN-5DF7THCD7UL.gmsaping.com

#1>     Client: WebApp02$ @ GMSAPING.COM
        Server: krbtgt/GMSAPING.COM @ GMSAPING.COM
        KerbTicket Encryption Type: AES-256-CTS-HMAC-SHA1-96
        Ticket Flags 0x40e10000 -> forwardable renewable initial pre_authent name_canonicalize
        Start Time: 5/21/2024 7:20:13 (local)
        End Time:   5/21/2024 17:20:13 (local)
        Renew Time: 5/28/2024 7:20:13 (local)
        Session Key Type: AES-256-CTS-HMAC-SHA1-96
        Cache Flags: 0x1 -> PRIMARY
        Kdc Called: WIN-5DF7THCD7UL.gmsaping.com
PS C:\>

```
