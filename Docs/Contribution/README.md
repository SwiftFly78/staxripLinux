# [Documentation](../README.md) / Contribution

Contributions should be small, focused and easy to verify. Search the existing issues and pull requests before starting
substantial work, and open an issue first when a change affects project compatibility, the behavior of external tools,
persisted settings or the packaging.

## Building

StaxRipLinux is built with the .NET 10 SDK, see [Building](../Introduction/Linux.md#building). A successful build
doesn't mean that every workflow runs: the encoders, frame servers, plugins and other tools come from the system, see
[Requirements](../Introduction/Linux.md#requirements). The C++ frame server in `Source/FrameServer`, AutoCrop in
`Source/Tools/AutoCrop` and the PowerShell build scripts in `Source` belong to the Windows version and aren't used.

## Commits

- Every commit contains one change, bug fixes and new features are never combined in one commit.
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org), like
  `fix(eac3to): detect MP2 tracks`. The type determines the version of the commit, see
  [Versioning](../Introduction/Linux.md#versioning).
- The message body describes what changed, why, and how it was tested.

## Pull requests

Before opening a pull request:

1. Rebase the branch on the current `master` branch of this repository.
2. Keep the branch limited to one change or a closely related set of changes.
3. Build StaxRipLinux, test the changed workflows and state in the description what was tested and what wasn't, for
   example tools, frame servers or hardware encoders that weren't available.
4. Don't include build outputs, personal settings, projects, media files or logs.
5. Explain changes to command lines, persisted settings, the selection of tools, cleanup, concurrency or native
   interfaces in the description.

The commits of a pull request get their version tags when they're merged.
