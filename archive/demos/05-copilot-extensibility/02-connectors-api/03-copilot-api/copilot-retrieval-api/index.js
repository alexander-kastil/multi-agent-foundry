const msalConfig = {
  auth: {
    clientId: config.clientId,
    authority: `https://login.microsoftonline.com/${config.tenantId}/`,
    redirectUri: 'http://localhost:8080',
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (!containsPii) {
          console.log(message);
        }
      },
      logLevel: msal.LogLevel.Verbose,
    },
  },
};

const loginRequest = {
  scopes: [
    'User.Read',
    'Files.Read.All',
    'Sites.Read.All',
    'ExternalItem.Read.All',
  ],
};

const retrievalEndpoint = 'https://graph.microsoft.com/beta/copilot/retrieval';
const retrievalScopes = [
  'Files.Read.All',
  'Sites.Read.All',
  'ExternalItem.Read.All',
];

const msalInstance = new msal.PublicClientApplication(msalConfig);

let msalInitialized = false;
const msalInitialization = msalInstance
  .initialize()
  .then(() => {
    msalInitialized = true;
    console.log('MSAL initialized successfully');
  })
  .catch((error) => {
    console.error('MSAL initialization failed', error);
    throw error;
  });

const dedupeScopes = (scopes) => Array.from(new Set(scopes.filter(Boolean)));

function getResultElement() {
  return document.getElementById('result');
}

function updateResult(content, { isError = false } = {}) {
  const resultElement = getResultElement();
  if (!resultElement) {
    return;
  }

  resultElement.classList.toggle('error', isError);

  if (typeof content === 'string') {
    resultElement.textContent = content;
    return;
  }

  resultElement.textContent = JSON.stringify(content, null, 2);
}

function serializeError(error) {
  if (error instanceof Error) {
    return {
      name: error.name,
      message: error.message,
      stack: error.stack,
      ...(error.errorCode ? { errorCode: error.errorCode } : {}),
    };
  }

  return error;
}

async function ensureMsalInitialized() {
  if (msalInitialized) {
    return;
  }

  await msalInitialization;
}

async function ensureActiveAccount(scopes) {
  await ensureMsalInitialized();

  let account = msalInstance.getActiveAccount();
  let accounts = msalInstance.getAllAccounts();

  if (!account && accounts.length === 0) {
    const loginResponse = await msalInstance.loginPopup({
      ...loginRequest,
      scopes: dedupeScopes([...loginRequest.scopes, ...scopes]),
    });

    account = loginResponse.account ?? null;
    if (account) {
      msalInstance.setActiveAccount(account);
    }

    accounts = msalInstance.getAllAccounts();
  }

  if (!account) {
    account = accounts[0] ?? null;
    if (account) {
      msalInstance.setActiveAccount(account);
    }
  }

  if (!account) {
    throw new Error('No active account available. Please sign in again.');
  }

  return account;
}

async function getAccessToken(scopes = loginRequest.scopes) {
  const normalizedScopes = dedupeScopes(
    scopes.length ? scopes : loginRequest.scopes
  );
  const account = await ensureActiveAccount(normalizedScopes);

  try {
    const response = await msalInstance.acquireTokenSilent({
      scopes: normalizedScopes,
      account,
    });
    return response.accessToken;
  } catch (error) {
    if (error instanceof msal.InteractionRequiredAuthError) {
      const response = await msalInstance.acquireTokenPopup({
        scopes: normalizedScopes,
        account,
      });
      return response.accessToken;
    }

    throw error;
  }
}

async function callRetrievalApi(exampleLabel, payload) {
  updateResult(`${exampleLabel} – requesting data...`);

  try {
    const accessToken = await getAccessToken([...retrievalScopes, 'User.Read']);
    const response = await fetch(retrievalEndpoint, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
    });

    const text = await response.text();
    let data;

    try {
      data = text ? JSON.parse(text) : {};
    } catch (parseError) {
      data = text;
    }

    if (!response.ok) {
      updateResult(
        {
          example: exampleLabel,
          status: response.status,
          ok: response.ok,
          response: data,
        },
        { isError: true }
      );
      return;
    }

    updateResult({
      example: exampleLabel,
      status: response.status,
      response: data,
    });
  } catch (error) {
    console.error(`${exampleLabel} failed`, error);
    updateResult(
      {
        example: exampleLabel,
        error: serializeError(error),
      },
      { isError: true }
    );
  }
}

async function doAuth() {
  updateResult('Signing in and loading profile...');

  try {
    const accessToken = await getAccessToken(loginRequest.scopes);
    const profileResponse = await fetch(
      'https://graph.microsoft.com/v1.0/me/',
      {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      }
    );

    const profile = await profileResponse.json();

    if (!profileResponse.ok) {
      updateResult(
        {
          status: profileResponse.status,
          response: profile,
        },
        { isError: true }
      );
      return;
    }

    updateResult({
      message: 'Authentication successful.',
      profile,
    });
  } catch (error) {
    console.error('Authentication failed', error);
    updateResult(
      {
        message: 'Authentication failed.',
        error: serializeError(error),
      },
      { isError: true }
    );
  }
}

async function runExample1() {
  await callRetrievalApi('Example 1 – SharePoint', {
    queryString: 'What extension patterns for Copilot do we know',
    dataSource: 'sharePoint',
    resourceMetadata: ['title', 'author'],
    maximumNumberOfResults: 10,
  });
}

async function runExample2() {
  await callRetrievalApi('Example 2 – Copilot connectors', {
    queryString: 'How to setup corporate VPN?',
    dataSource: 'externalItem',
    resourceMetadata: ['title', 'author'],
    maximumNumberOfResults: 10,
  });
}

async function runExample4() {
  await callRetrievalApi('Example 4 – SharePoint site filter', {
    queryString: 'What extension patterns for Copilot do we know',
    dataSource: 'sharePoint',
    filterExpression:
      'path:"https://integrationsonline.sharepoint.com/sites/copilot-demo/"',
    resourceMetadata: ['title', 'author'],
    maximumNumberOfResults: 4,
  });
}
