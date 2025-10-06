# DirectoryDoppelganger
Simple C# app that watches specified directory and creates its exact copy where specified. Can be used both as standalone tool or as an API.

Options are currently set via Command Line Arguments that are passed down in constructor, so injecting custom values from script is possible.


## Options:

Format of arguments is name followed by value (if needed) seperated by spaces. Options can be set in any order and are not case sensitive.

### Example Command Line Arguments:

-watched C:\ExampleDir\Watched -copy C:\ExampleDir\Copy -log C:\ExampleDir\ -t 5 -bytebybyte

### Update Delay
Description: Time in seconds between updates when utilizing periodic update mode. Optional, 10 seconds are used when not set.

Valid names: "-update", "-updatedelay", "-updatetime", "-time", "-delay", "-t", "-d"

Value: float (seconds)

### Watched Directory
Description: Directory that is monitored and its contents will be duplicated to Copy-To directory. Required.

Valid names: "-watched", "-watcheddir", "-watcheddirectory", "-wd"

Value: string (path)

### Watched Directory
Description: Directory that contents of Watched directory are copied into. Required. If it doesn't exist, but path is valid, it will be created during initialization.

Valid names: "-copy", "-copydir", "-copydirectory", "-copyto", "-copytodir", "-copytodirectory", "-cd", "-ctd"

Value: string (path)

### Logs Directory
Description: Directory where logs will be created and store. Optional, logs will not be created if not set. If directory doesn't exist, but path is valid, it will be created during initialization.

Valid names: "-log", "-logdir", "-logdirectory", "-ld" 

Value: string (path)

### Use ByteByByte Comparison
Description: Toggle that makes application use byte by byte comparison mode of evaluation whether files are identical. Overall worse performance, but delivers 100% certain results. See performance test results below.

Valid names: "-bytebybyte", "-bbb"

Value: none



## MD5 vs ByteByByte performance test
Measurements gathered from 50 update cycles

### 3000x 100KB .PNGs ---------------------
**Byte by byte**

avg: 2637ms

max: 2771ms

min: 2596ms

**MD5**

avg: 1420ms

max: 1459ms

min: 1397ms

### 1x 249MB .MP4 --------------------------
**Byte by byte**

avg: 2025ms

max: 2081ms

min: 2013ms

**MD5**

avg: 991ms

max: 998ms

min: 984ms

### 25000x 10KB .TXTs ----------------------
**Byte by byte**

avg: 11835ms

max: 12460ms

min: 11669ms

**MD5**

avg: 5028ms

max: 5308ms

min: 4946ms
