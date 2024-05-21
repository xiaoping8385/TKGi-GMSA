
1.Configure TKGi, add the windows server as Node DNS from the network configuration, apply the change and upgrade the cluster.
Another simple way is to edit the /etc/resolv.conf, to add the nameserver info.
Eg.
kubo@jumper:~$ cat /etc/resolv.conf 
nameserver 10.199.17.13
nameserver 127.0.0.1
nameserver 10.21.82.1
search eng.vmware.com nimbus.eng.vmware.com vmware.com
kubo@jumper:~$ 

2.Add your container host to the domain:
   From your 