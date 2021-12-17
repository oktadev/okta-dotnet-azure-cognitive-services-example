# .NET + Azure Cognitive Services

This tutorial shows you how to call Azure Cognitive Services to verify faces and store profiles pics in Azure Storage in a .NET MVC app.

Please read [Using Azure Cognitive Services in a .NET App][blog] to walk through the tutorial.

**Prerequisites**

* Basic knowledge of C#
* [.NET 5.0 runtime and SDK](https://dotnet.microsoft.com/en-us/download/dotnet/5.0), which includes the .NET CLI
* Your favorite IDE that supports .NET projects, such as [Visual Studio](https://visualstudio.microsoft.com/downloads/), [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/), [VS Code](https://code.visualstudio.com/), or [JetBrains Rider](https://www.jetbrains.com/rider/)
* [Okta CLI](https://cli.okta.com/)
* A [Microsoft Azure Account](https://azure.microsoft.com/en-us/free/) (Azure free account)

> [Okta](https://developer.okta.com/) has Authentication and User Management APIs that reduce development time with instant-on, scalable user infrastructure. Okta's intuitive API and expert support make it easy for developers to authenticate, manage and secure users and roles in any application.

## Getting Started

To install this example applications, run the following commands:
```shell
git clone https://github.com/oktadev/okta-dotnet-azure-cognitive-services-example.git
cd okta-dotnet-azure-cognitive-services-example
```

### Create an OIDC Application in Okta

Create a free developer account with the following command using the [Okta CLI](https://cli.okta.com):

```shell
okta register
```

If you already have a developer account, use `okta login` to integrate it with the Okta CLI.

Provide the required information. Once you register, create a client application in Okta with the following command:

```shell
okta apps create
```

You will be prompted to select the following options:
- Type of Application: **1: Web**
- Redirect URI: `https://localhost:5001/authorization-code/callback`
- Logout Redirect URI: `https://localhost:5001/signout/callback`

Run cat .okta.env (or type .okta.env on Windows) to see the issuer and credentials for your app.

Update `appsettings.Development.json` with your Okta settings.

```json
{
  "Logging": {
    "LogLevel": {
    "Default": "Information",
    "Microsoft": "Warning",
    "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Okta": {
    "ClientId": "{clientId}",
    "ClientSecret": "{clientSecret}",
    "Domain": "https://{yourOktaDomain}"
  }
}
```

For remaining instructions on creating an API token, custom attributes, and Azure resources, please follow the tutorial.

## Help

Please post any questions as comments on the [blog post][blog], or visit our [Okta Developer Forums](https://devforum.okta.com/).

## License

Apache 2.0, see [LICENSE](LICENSE).

[blog]: https://developer.okta.com/blog/2022/01/12/net-azure-cognitive-services



