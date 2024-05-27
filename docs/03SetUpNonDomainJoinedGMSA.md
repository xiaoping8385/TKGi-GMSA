1. In your domain Controller, create the security group, gmsa account, and a gmsa user account, add the user account to the security group
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
2. 
