### PolMon - Funny Mini Server Monitor

Mini server monitoring tool for Windows that provides real-time CPU, GPU, RAM, disk, and network statistics via a simple HTTP server. Designed for system administrators, it uses OpenHardwareMonitor to track hardware performance, including CPU/GPU temperatures and fan speeds. PolMon also serves data in JSON format and has a web-based dashboard

**Usage:**
```bash
polmon.exe [options]

-port <number>	Set the listening port	8080
-ip <address>	Set the IP address to listen on	127.0.0.1
-rtime <number>	Set the refresh time in milliseconds	500
-help, --help	Display this help message	
