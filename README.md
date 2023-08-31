# Blackbird.io XTM

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

XTM Cloud is a translation management system. Features include the creation of projects and uploading files to these projects in order to translate them.

## Before setting up

Before you can connect you need to make sure that:

- You have an XTM Cloud instance with **API access enabled**.

## Connecting

1. Navigate to apps and search for XTM. If you cannot find XTM then click _Add App_ in the top right corner, select XTM and add the app to your Blackbird environment.
2. Click _Add Connection_.
3. Name your connection for future reference e.g. 'My XTM'.
4. For client fill in your XTM Company name, also marked client in the [XTM documentation](https://api.xtm-cloud.com/rest-api/#tag/XTM-Basic/operation/generateToken).
5. For User ID fill in your numeric XTM user ID. Note: **This is not your username**. A known way to retrieve this user ID is to log into your XTM portal, hover on your avatar in the top right corner and right click "open image in new tab". Then in the URL of this page you will find your user ID.\*
6. Fill in your XTM user password.
7. Finally fill in the base URL of your XTM instance. F.e. `https://xtm.mycompany.com`
8. Click _Connect_.

\* Do you know a better way to retrieve the User ID? Let us know!

![connecting](image/README/1693487780830.png)

## Actions

### Projects

- **List projects**
- **Get project**
- **Create project**
- **Create project from template**
- **Clone project**
- **Update project**
- **Add project target languages**
- **Delete project target languages**
- **Reanalyze project**
- **Delete project**
- **Get project estimates**

### Files

- **Download source files**
- **Download source files as ZIP**
- **Download project file**
- **Upload source file**
- **Upload translation file**

### Translation memories

- **Generate TM file**
- **Download TM file**

### Customers

- **List customers**
- **Create customer**
- **Get customer**
- **Update customer**
- **Delete customer**

## Events

- **On analysis finished**
- **On workflow transition**
- **On job finished**
- **On project created**
- **On project accepted**
- **On project finished**
- **On invoice status changed**

_Due to a technical limitation, some of these events do not contain any data at the moment_

## Missing features

The current implementation covers the basic actions. However, in the future we can also support:

- Project LQA
- Project analytics
- Custom fields
- Jobs
- External users

Let us know if you're interested!

## Feedback

Feedback to our implementation of XTM is always very welcome. Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.
