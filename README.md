# Description
This project provides:
- A converter to generate a Swagger 2.0 definition based on a OData-Meta-Definition
- A policy which makes it possible to route OData specific queries through the API-Manager. 

The converter takes in an OData-Metadata-File (such as http://services.odata.org/V4/TripPinServiceRW/$metadata) from either a File/URL and converts it into a Swagger 2.0 API-Specification, which can imported into the Axway API-Manager.
Once imported and virtualized as a Frontend-API you need make the Policy provided in this project a routing policy, as it takes care about proper routing all different OData-Requests.

Note 27.03.2018: Providing the OData-Swagger-Converter is still in progress!

## API Management Version Compatibilty
This artefact was successfully tested for the following versions:
- V7.5.3


## Install

```
• Use the Converter to generate a Swagger 2.0 API-Sepcification
• Import the Policy: "OData routing" into you API-Manager configuration group
• Make this policy part of your API-Manager routing policy (e.g. as a Policy-Shortcut)
• Configure your routing policy in API-Manager for your OData-Frontend API
```

## Usage

```
• To enable this specific routing for your APIs, make sure you select the configured routing policy
• Configured API-Methods must end with * (example: ..../Airports* without any slash)
  (done by the Converter in that way)
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
