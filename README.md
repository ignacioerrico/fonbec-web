# FONBEC Web
[![MIT License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ignacioerrico/fonbec-web/blob/main/LICENSE.txt)

![Castellano](https://github.com/madebybowtie/FlagKit/raw/master/Assets/PNG/AR@2x.png?raw=true)

**Castellano**

[FONBEC](http://www.fonbec.org.ar/) es una fundación de Argentina que conecta
dos mundos: el de chicos que quieren estudiar (_becarios_) y el de personas que
los quieren ayudar (_padrinos_ y _madrinas_).

Uno de los [pilares de FONBEC](https://www.fonbec.org.ar/?page_id=12) es la
**carta**, que incentiva la relación entre padrino y ahijado.  De esta manera,
el ahijado no es un ser anónimo.

Hay fechas preestableciadas para el envío de una carta por parte de los
becarios, y un grupo de _voluntarios_ se encarga de leerlas (para detectar
necesidades o problemas que puedan existir) y de enviárselas al padrino o
madrina correspondiente.  Ese proceso se estuvo haciendo de manera manual, lo
que no solo demanda tiempo, sino que también es proclive a errores humanos
(cartas que no se envían, cartas que se envían a la persona incorrecta, etc.).

Uno de los objetivos de este proyecto es automatizar ese proceso para que cada
persona que interviene pueda enfocarse en su rol.

![English](https://github.com/madebybowtie/FlagKit/raw/master/Assets/PNG/US@2x.png?raw=true)

**English**

[FONBEC](http://www.fonbec.org.ar/) is a nonprofit organization based in
Argentina that connects students who want to continue their education but lack
the necessary resources (_grantees_) with individuals who want to support them
(_benefactors_).

One of the organization's core pillars is the **letter**, which fosters a
personal relationship between grantees and benefactors, ensuring that grantees
are not treated as "anonymous entities."

Throughout the year, there are designated dates for grantees to send letters to
their benefactors. A team of _volunteers_ reviews these letters to identify any
needs or concerns the students may have. We then forward the letters to the
appropriate benefactor. This process has been done manually, which not only
makes it time-consuming but also prone to errors (such as undelivered letters or
letters sent to the wrong recipient).

A key objective of this project is to automate this process, enabling everyone
involved to focus more effectively on their specific roles.

## Setting up your development environment

The solution is implemented with ASP.NET Core Blazor 9, so the [.NET 9.0
SDK](https://dotnet.microsoft.com/en-us/download) is required. If you're using
[Visual Studio 2022](https://visualstudio.microsoft.com/) you should be all set.

It uses Entity Framework Core with a code-first approach and is configured to
use **SQL Server**. If you use Visual Studio, just make sure the _Data storage
and processing_ workload is enabled. (Visual Studio Installer → Modify →
Workloads → Data storage and processing). Otherwise, install [SQL Server
2022](https://www.microsoft.com/en-us/sql-server/sql-server-downloads).

In `appsettings.json`, set `Server` in the `FonbecWebDbContextConnection`
connection string to a SQL Server instance that exists on your machine. To list
the SQL Server instances on your machine, execute this command:

`sqllocaldb i`

You may also use a different database name.

**Don't commit any changes you make to the connection string.**

In the _Package Manager Console_, execute this command to update the database:

`Update-Database`

The following data is seeded in the corresponding tables following the [official
recommendations](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding):
- Roles
- Default user (admin)

The different roles are hard-coded, since they respond to a business
requirement.

The default user, which has a role of Admin, is created based on a configurable
username and password. For security reasons, those should be set as a secrets
and never exposed in version control. Add them as user secrets (right-click on
the UI project, `Fonbec.Web.Ui` &rarr; Manage User Secrets) with this format:

```
"AdminUser": {
  "Username": "",
  "Password": ""
}
```

Assign a value to each field.