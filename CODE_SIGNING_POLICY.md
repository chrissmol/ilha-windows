# Code signing policy

## Status

**Application submitted on 2026-11-15; awaiting review.** Ilha has not yet been accepted by SignPath Foundation and no SignPath certificate is currently available to this project. Unsigned artifacts are labelled as such. Publishing the source alone does not remove Smart App Control restrictions.

If accepted, the project will display the required attribution: “Free code signing provided by SignPath.io, certificate by SignPath Foundation”. This statement is conditional until service has actually been granted.

## Roles

- Project maintainer, committer and release approver: `chrissmol`.
- New contributors submit pull requests for review.
- Signing requests require manual approval under the signing provider's configured policy.
- Maintainer accounts used for signing must have multi-factor authentication enabled.

## Build and release rules

1. Build from the public repository on a GitHub-hosted Windows runner.
2. Run the application checks and package only explicitly selected distribution files.
3. Upload the unsigned ZIP as a GitHub Actions artifact.
4. When SignPath is configured, a maintainer can manually request signing for a version tag.
5. Verify Authenticode signatures on every executable and runtime PowerShell script before labelling an artifact signed.
6. Publish signed artifacts only after successful verification. Ordinary pushes and pull requests do not request signing.

Product name: `Ilha`. Product/file version is defined once in `AssemblyInfo.cs` for both the launcher and runtime.

No signing keys, API tokens, personal certificates or private runtime data belong in the repository.

## Privacy

See [PRIVACY.md](PRIVACY.md).

## References

- [Foundation requirements](https://signpath.org/terms.html)
- [Application](https://signpath.org/apply)
- [GitHub integration](https://docs.signpath.io/trusted-build-systems/github)
- [Smart App Control signing](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control)
