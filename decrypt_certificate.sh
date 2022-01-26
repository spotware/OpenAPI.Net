#!/bin/sh

# Decrypt the file
mkdir $HOME/secrets
# --batch to prevent interactive command
# --yes to assume "yes" for questions
gpg --quiet --batch --yes --decrypt --passphrase="$CERTIFICATE_GPG_PASSPHRASE" \
--output $HOME/secrets/certificate.pfx certificate.pfx.gpg