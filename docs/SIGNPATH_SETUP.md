# SignPath setup after repository publication

The Foundation application was submitted on 2026-11-15 and the form confirmed receipt. Account activation, approval and certificate access are pending. Publication and Foundation acceptance are separate steps. The Foundation evaluates project reputation and other requirements and may decline a new project.

1. Publish the source repository and review the MIT license, project status, privacy policy and signing policy.
2. Let the unsigned GitHub-hosted build run and inspect its checks and artifact.
3. Make an initial preview release clearly labelled **unsigned / development preview**. A working signed release is not yet available.
4. Apply at https://signpath.org/apply with the actual repository URL, project description and maintainer identity. The application form may require information that must come from the maintainer.
5. After acceptance, configure the SignPath GitHub App for this repository and create the project/artifact configuration in SignPath.
6. Configure the archive to sign `Ilha.exe`, the versioned `IlhaGlass.exe`, `MediaBridge.ps1` and `ClaudeHook.ps1`. The GitHub artifact contains the distribution ZIP, so the archive configuration must cover that nested ZIP while preserving filenames and directories.
7. Enforce product name `Ilha` and consistent product/file versions for both PE executables. Configure the production signing policy with manual release approval.
8. Add repository secret `SIGNPATH_API_TOKEN` and variables `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_SIGNING_POLICY_SLUG` and, if required by the account, `SIGNPATH_ARTIFACT_CONFIGURATION_SLUG`.
9. Dispatch the build workflow for a `v…` tag with signing enabled. Review and approve the request in SignPath.
10. Inspect the signed artifact and the signature-verification result before publishing a signed release.

The maintainer explicitly approved submitting the application and accepting the program terms and personal-data processing consent. The source files do not submit applications or accept account permissions.
