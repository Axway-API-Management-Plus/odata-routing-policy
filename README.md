# Description
This project provides:
- A converter to generate a Swagger 2.0 definition based on a OData-Meta-Definition
- A policy which makes it possible to route OData specific queries through the API-Manager. 

The converter takes in an OData URL (such as http://services.odata.org/V4/TripPinServiceRW) and the name of the output file to convert the odata specfication to a Swagger 2.0 API-Specification, which can imported into the Axway API-Manager.
## OData V4 Usage:
```bash
D:\odata-routing-policy\converter\odata4\bin\Release>OData2Swagger.exe http://services.odata.org/V4/TripPinServiceRW name_of_swagger_file.json [http-basic-username] [http-basic-password]

```

## OData V3 and Below Usage:
```bash
D:\odata-routing-policy\converter\odata3\bin\Release>OdataSwaggerConverter.exe http://services.odata.org/V3/Northwind/Northwind.svc/$metadata name_of_swagger_file.json [http-basic-username] [http-basic-password]

```

The converter generates a Swagger-File using the given name in the current folder.
Please note, that providing username/password is optional.


Once imported and virtualized as a Frontend-API you need make the Policy provided in this project a routing policy, as it takes care about proper routing all different OData-Requests.

To 

## API Management Version Compatibilty
This artefact was successfully tested for the following versions:
- V7.5.3


## Install

```
• Use the Converter to generate a Swagger 2.0 API-Sepcification
• Import the Policy: "OData routing" into you API-Manager configuration group
• Make this policy part of your API-Manager routing policy (e.g. as a Policy-Shortcut)
```
![Routing Policy](https://github.com/Axway-API-Management-Plus/odata-routing-policy/blob/master/images/OData-Policy-Linked-to-Routing.png)
```
• Configure your routing policy in API-Manager for your OData-Frontend API
• Important note: When not using the provided Routing-Policy from this project: 
     Verify, in the "Connect to URL" filter that the URL-Parameter is configured to ${destinationURL}
```

## Usage

```
• To enable this specific routing for your APIs, make sure you select the configured routing policy
```
![API-Manager Routing Policy](https://github.com/Axway-API-Management-Plus/odata-routing-policy/blob/master/images/api-routing-policy-incl-odata.png)
```
• Configured API-Methods must end with * (example: ..../Airports* without any slash)
  (done by the Converter in that way)
 ```
![API-Manager OData API](https://github.com/Axway-API-Management-Plus/odata-routing-policy/blob/master/images/odata-api-in-apimgr.png)
```
• And correct the FE-API Path by removing this part (S(bwlyi5zyfdifrulnwfpj2f4u)): 
```
![API-Manager OData FE_API](https://github.com/Axway-API-Management-Plus/odata-routing-policy/blob/master/images/odata-api-in-apimgr-fe-api.png)

## Bug and Caveats

When using nested query parameters like in this example:
```
/People?$expand=Trips($filter=Name eq 'Trip in US')
```
the nested parameter must be URL-Encoded due to the fact, how the API-Manager 
parses the Query-Parameters based on the equal ("=") sign:
```
/People?$expand=Trips(%24filter%3DName%20eq%20%27Trip%20in%20US%27)
```
Besides that, the Postman-Test-Suite passes including many differant query variations. 
If you contribute, make sure the test-suite still runs okay.

## Contributing

Please read [Contributing.md](https://github.com/Axway-API-Management/Common/blob/master/Contributing.md) for details on our code of conduct, and the process for submitting pull requests to us.


## Team

![alt text][Axwaylogo] Axway Team

[Axwaylogo]: https://github.com/Axway-API-Management/Common/blob/master/img/AxwayLogoSmall.png  "Axway logo"


## License
[Apache License 2.0](/LICENSE)
