#SimpleFtp

|            | Production | Dev |
| ----------:| ---------- | --- |
| AppVeyor   | [![Build status](https://ci.appveyor.com/api/projects/status/najmvwr4ff7l637x?svg=true)](https://ci.appveyor.com/project/PureKrome/simpleftp-33rxa) | [![Build status](https://ci.appveyor.com/api/projects/status/3uegfn36fu0hw2ji?svg=true)](https://ci.appveyor.com/project/PureKrome/simpleftp)
| NuGet      | [![NuGet Badge](https://buildstats.info/nuget/SimpleFtp)](https://www.nuget.org/packages/SimpleFtp/) | [![MyGet Badge](https://buildstats.info/myget/simpleftp/simpleftp)](https://www.myget.org/feed/simpleftp/package/nuget/simpleftp) |

---

### SimpleFtp

This library is a simple interface and wrapper-class for the `System.Net.WebClient` class. This helps make testing easier (e.g. you can inject this into your services to remove any hard integration dependencies).

### Usage.

1. `install-package simpleftp` (choose from NuGet (for stable) or MyGet (for development)) 
2. add some code to where u wish to use it

```

// Note: safety checks, etc are obmitted for brevity.

private readonly IFtpService _ftpService;

public SomeService(IFtpService ftpService)
{
    _ftpService = ftpService;
}

public async Task SomeMethodAsync()
{
    var someFileContent = "whatever";
    var someFileName = "whatever.txt";

    await _ftpService.UploadAsync(someFileContent, someFileName);
}

// --------------------------------

var loggingService = new LoggingService();
var ftpService = new FtpService("ftp.blahblah.com", "someUsername", "somePassword", loggingService);
var someService = new SomeService(ftpService);

```

Hope this helps!

---
[![I'm happy to accept tips](http://img.shields.io/gittip/purekrome.svg?style=flat-square)](https://gratipay.com/PureKrome/)  
![Lic: MIT](http://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)
