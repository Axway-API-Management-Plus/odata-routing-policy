# Description
A policy which makes it possible to route OData specific queries through the API-Manager. 

## API Management Version Compatibilty
This artefact was successfully tested for the following versions:
- V7.5.3


## Install

```
• Import the Policy: "OData routing" into you API-Manager configuration group
• Make this policy part of your API-Manager routing policy (e.g. as a Policy-Shortcut)
• Configure your routing policy in API-Manager
```

## Usage

```
• To enable this specific routing for your APIs, make sure you select the configured routing policy
• Configured API-Methods must end with * (example: ..../Airports* without any slash)
```

## Bug and Caveats

```
No known bugs or caveats. 
Test-Suite test all possible variations. If you contribute, make sure the test-suite still runs okay.
```

## Contributing

Please read [Contributing.md](https://github.com/Axway-API-Management/Common/blob/master/Contributing.md) for details on our code of conduct, and the process for submitting pull requests to us.


## Team

![alt text][Axwaylogo] Axway Team

[Axwaylogo]: https://github.com/Axway-API-Management/Common/blob/master/img/AxwayLogoSmall.png  "Axway logo"


## License
[Apache License 2.0](/LICENSE)
