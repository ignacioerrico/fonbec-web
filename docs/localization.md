# UI localization — AB#110

## Summary

The first localization phase makes the application shell, manager home, sign-in
and the facilitator/reviewer user-management pilot available in Spanish and
English. Spanish remains the production default. User names, chapter names,
stored notes, role identifiers, routes and authorization policy names remain data.

## User experience

Open `/?culture=es` or `/?culture=en` with a full page load. The explicit choice
is carried in the standard ASP.NET Core session culture cookie so navigation,
redirects, static Account form posts and the Interactive Server connection agree.
There is no language-picker UI or per-user database preference. A fresh browser
without a culture cookie starts in Spanish even if its browser language is English.
Default formatting is `es-AR`; UI resources use `es`. Unsupported cultures fall
back through the configured request-culture providers to the Spanish default.

Use a full reload after changing culture: an existing interactive circuit keeps
its established culture. To clear a development override, use `?culture=es` or
remove the `.AspNetCore.Culture` session cookie.

## Technical overview

- `Localization/LocalizationSetup.cs` registers localization, the MudBlazor
  adapter and request culture providers. Middleware runs after forwarded headers
  and before authentication and component endpoints, including static SSR.
- `Resources/Localization/SharedResources.es.resx` contains production Spanish.
- `SharedResources.en.resx` contains explicit English translations.
- Neutral `SharedResources.resx` contains the same English text. ASP.NET Core's
  resource fallback resolves a missing Spanish key to neutral English, then returns
  the key when neither translation exists. Do not put Spanish in the neutral file.
- `IStringLocalizer<SharedResources>` is injected only into migrated components.
  Components' partial C# classes use the same injected property.
- `ValidationResources` provides static resource properties for DataAnnotations
  on the Login and ForgotPassword form models; static SSR form handling is retained.
- `FonbecMudLocalizer` supplies grid, filter, editing and pager translations.
  Unmapped MudBlazor keys retain the library's built-in English fallback.
- `App.razor` sets `html lang` from the active UI culture.

No database, Identity user schema, external-provider configuration, permission
policy or application API contract changes are required. Existing service/Identity
error descriptions are displayed as returned; migrating those requires a separate
domain-error localization contract in phase 2.

## Adding or updating text

1. Choose a descriptive English PascalCase key such as `CreateUserFailed`.
   Use complete sentences with `{0}` placeholders instead of concatenating words.
2. Add English text to **both** neutral and `.en.resx` resources.
3. Add the same key and compatible placeholders to `.es.resx`. Spanish is the
   authoritative production copy; omission is supported temporarily via fallback.
4. In Razor, inject `IStringLocalizer<SharedResources> L` and use `@L["Key"]`
   or `@L["Key", argument]`. Use `Label="@L["Key"]"` for component attributes.
5. Keep translation lookups at render/use time. Do not cache translated strings
   in static fields or translate role/policy identifiers used for authorization.
6. Verify both languages, missing-key fallback and `html lang`. Review migrated
   files for new literal UI strings, including accessibility labels and tooltips.
   This PR uses review rather than a build-blocking string-literal analyzer.

Shared keys are appropriate for this pilot. For a larger feature, add its own
marker type under `Localization/` and matching resources under
`Resources/Localization/`; the built-in `IStringLocalizer<T>` follows the same
neutral-English convention. Translators can edit resources independently of Razor.

## Phase coverage and remaining work

Phase 1 includes NavMenu and its child links, MainLayout information/theme/error
copy, manager Home and campaign summary, Login, ForgotPassword and confirmation,
Lockout, ExternalLoginPicker, LoadingIndicator, selectors, user audit tooltips,
email/phone accessibility labels, and RoleUsersList/RoleUserCreate with facilitator
and reviewer wrappers. PageHeader is a data-only component: migrated callers pass
localized titles to its existing Title parameter. The `FONBEC Web` brand is a
proper name in both languages.

**Phase 2 is a separate follow-up, not part of this implementation's completion:**
remaining business pages, Admin user-management pages, other Account/Manage pages,
external-login result pages, permission metadata, Logic/DataAccess display strings,
service/Identity error descriptions, and remaining third-party component keys.
Transactional email translation, stored user content, per-user language settings
and extra languages remain out of scope. Team review must confirm this split and
the shared-resource naming convention before the overall story is closed. Publish
this guide in the team wiki after review; this repository copy is the versioned
source for that page.

## Validation

- `dotnet test --solution Fonbec.Web.sln --configuration Release` using SDK 10.0.402
  and Microsoft.Testing.Platform; Azurite is required for the blob integration test.
- Localization tests cover Spanish/English lookup, missing Spanish → neutral
  English → key fallback, Argentine default formatting, unsupported culture,
  culture cookie reuse and MudBlazor/DataAnnotations translations.
- Local browser checks with synthetic Manager/facilitator data: English Home,
  navigation without query parameters, user editing and persistence after reload,
  Spanish grid/filter/pager copy, English create form, sign-out, empty Login POST
  validation and successful Login POST retaining English and `html lang=en`.
- Real Google/Facebook OAuth round trips and independent QA acceptance remain
  pending; providers were not configured in the isolated local environment.

## Rollback

Revert this feature commit. There are no migrations or data conversions. A leftover
culture cookie has no effect when request localization is removed.
