# Microsoft Identity Getting Started

[MSAL Authentication Flows](https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-authentication-flows)

[Decode Token](http://jwt.ms/)

[az ad app](https://docs.microsoft.com/en-us/cli/azure/ad/app?view=azure-cli-latest)

## Demos

- Manage App Registration using Azure CLI
  - Examine `create-app-reg.azcli`
- Explain Token Types

### Run Demo

Replace tenant id und client id in `index.js`:

```javascript
const msalConfig = {
  auth: {
    clientId: 'eeb155cb-d4c6-4864-9184-cf10a6e02715',
    authority:
      'https://login.microsoftonline.com/d92b247e-90e0-4469-a129-6a32866c0d0a/',
    redirectUri: 'http://localhost:8080',
  },
```

Install http-server:

```
npm i -g http-server
```

> Note: Requires [Note.js](https://nodejs.org/download/release/v10.23.0/)

Run project:

```
cd ./token-flow-node
npm i
http-server
```

> Note: Use http://localhost:8080/ as this is used in the App Registration

Consent Screen:

![consent](_images/consent.jpg)

### Create App Registration using Azure CLI

Examine and execute `create-app-reg.azcli`
