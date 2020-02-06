# TwinCAT-HTTP-Server
Access to TwinCAT 3 data via http requests and json. This let's you easily read and write data and thus control TwinCAT workflows from anywhere in your network using any programming language or a browser.You can also test and plan complex json requests in the manual request mode.

![alt text](https://github.com/HeinzBenjamin/TwinCATAds-HTTPServer/blob/master/screenshot.jpg "TwinCATAds HTTP Server screenshot")

# Installation
Download the latest Release. Put everything in one folder on your computer.

# Usage
1. Run the 'Start_TwinCAT-Http-Server' shortcut in the main folder with your TwinCAT 3 project running on the same machine (admin right required).
2. In the upper left corner, define the TwinCAT address and port. Hit "Connect" to connect to TwinCAT. The right log text box shows all messages regarding this backend connection between TwinCAT-HTTP-Server and TwinCAT
3. Set port for local http server. Hit "Start" to start listening for incoming http requests. The left log text box shows all messages regarding incoming Http messages and the server itself.
4. You can now make requests from anywhere in the network using Http GET or POST requests.
5. To familiarize yourself with the required json structure chose an example in the lower left drop down menu.
6. Edit this example to your needs and send manual requests by clicking "Send" for testing.
(7. Under development: Copy this manual request in URL form to the clipboard by clicking the 'Copy as URL' button. Paste it to a browser to test the connection)
8. You can now control TwinCAT from your host machine using http requests in [Python](https://www.geeksforgeeks.org/get-post-requests-using-python/), [JavaScript](https://www.freecodecamp.org/news/here-is-the-most-popular-ways-to-make-an-http-request-in-javascript-954ce8c95aaa/), [C#](https://stackoverflow.com/questions/27108264/c-sharp-how-to-properly-make-a-http-web-get-request), [Postman](https://www.getpostman.com), a [browser](https://www.opera.com) or whatever else can speak HTTP.
9. Code snippets to make correct requests for all kinds of programming languages can be found in the 'RequestExamples' folder.

## Info
TwinCAT-Http-Server internally starts a restful ASP.NET WebAPI. This should take care of all network configurations and open the relevant ports to your host machine.
However, if you experience problems such as 'Access denied' or similar, check you network configuration using thtse steps:
This will most likely involve [opening the port on the host machine's firewall and defining a binding in the IIS manager](https://stackoverflow.com/questions/22044470/bad-request-invalid-hostname-while-connect-to-localhost-via-wifi-from-mobile-ph)<br>
To get the IIS manager to work use [this tutorial](https://stackoverflow.com/questions/30901434/iis-manager-in-windows-10).<br>
Try to ping between the host machine and the remote machine.<br>
By default TwinCAT-HTTP-Server will listen to the specified port number on all IPs. So it is up to you which IPs you [open up for listening using netsh](https://stackoverflow.com/questions/47969786/c-sharp-httplistener-the-format-of-the-specified-network-name-is-not-valid).<br>
A combination of these things and googling should do the trick for your specific network.

When this works you will be able to read and write TwinCAT variables from any http capable device in your network.<br><br>

# Get code
`git clone https://github.com/HeinzBenjamin/TwinCAT-HTTP-Server.git`

# Acknowledgement
Special thanks to [Tran Dang](https://www.uts.edu.au/staff/trantuananh.dang) at the University of Technology Sydney for great support and insight into TwinCAT 3 and the EtherCAT ecosystem.
This project was funded by the [German Research Exchange Service](https://www.daad.de/en/) (DAAD)
