# Certificates

Drop your client `.pfx` / `.p12` certificate file(s) here. The Certificate tab's file picker opens
this folder by default when you choose "Browse..." for a `.pfx` file.

**Nothing in this folder is committed to source control** except this README — see the `.gitignore`
next to it. Certificate files and their passwords should never end up in version control.

## Alternative: Windows Certificate Store

If your organization's policy prohibits exporting private keys to a `.pfx` file on disk, use the
"Windows certificate store" option on the Certificate tab instead. Import the certificate into your
Windows account's certificate store (`CurrentUser\My`) first (e.g. via `certmgr.msc` or your IT
department's provisioning process), then select it from the app - the private key never leaves the
store.

## Password handling

- The certificate password is never written to disk in plain text.
- If you save a scenario file and check "Remember password", the password is encrypted with Windows
  DPAPI, tied to your Windows account and this machine. A scenario file copied to another machine or
  opened by another user will not auto-fill the password - you'll just be prompted to re-enter it.
