ZoneEdit Dynamic Dns Client
+++++++++++++++++++++++++++

This is a Dynamic Dns IP Update Client for the ZoneEdit DNS service. This runs
as a self-installing Windows service built on top of the .NET Framework. It
checks for IP updates once per minute, and communicates any new changes to
ZoneEdit.

Similar FREE clients exist, but each has flaws:
* http://sourceforge.net/projects/zedyn/ - does not run as service, ancient
* http://www.freymond.ca/ZoneEditDynDNS/ - no longer works in Vista, 7

Included files
==============
DynDnsClient.zip        - application files
READ_ME.txt             - this document

Installation
============
1. Download and unzip DynDnsClient.zip
2. Configure application by editing DynDnsClient.exe.config
3. To install and run service, execute as administrator: DynDnsClient.exe -i

Other info:
* To uninstall and stop service, execute as administrator: DynDnsClient.exe -u
* To run in console, execute: DynDnsClient.exe -c
* Status information will be emitted to DynDnsClient.log

Known problems/limitations
==========================
1. Aggressive IP updates (e.g. due to frequent service restarts) cause
   ZoneEdit to return intentional status 702 errors. The application will back
   off automatically, but when this happens updates may be delayed.

Please direct questions and comments to:
Martijn Stevenson <martijn at alum dot mit dot edu>
