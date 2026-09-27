# Security Policy

Graft's agent opens a named pipe inside the app under test. Leaving that pipe in a production build is a security hole, so enablement has three gates:

1. Compile time: the `Agent.Start` API does not exist outside `GRAFT_TEST`
2. Analyzer: a reference without `GRAFT_TEST` is **GRAFT001** (error)
3. Run time: the pipe stays down unless `GRAFT_ENABLE=1`

`Application.LaunchAsync` sets the environment variables, including the pipe name and token. Do not set `GRAFT_ENABLE` outside a test.

## Reporting

Report vulnerabilities through **GitHub Security Advisories** ([Report a vulnerability](https://github.com/YUKIKEDA/Graft/security/advisories/new)). Do not put steps that bypass pipe authentication, or that mix the agent into production, in a public Issue.

Response targets (best effort):

- Acknowledge: within a few days
- Fix direction: depends on severity
