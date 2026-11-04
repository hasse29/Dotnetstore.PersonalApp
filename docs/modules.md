# Modules

Each module is one project under `src/Modules/<Module>/` with its own SQLite database in `%LOCALAPPDATA%/Dotnetstore.PersonalApp`. Modules never reference each other; they share only contracts in `Shared` (see [architecture.md](architecture.md)). Error codes are prefixed per module, so resource keys `Error_{code}` never collide.

| Module | Database | Owns | Sidebar |
| --- | --- | --- | --- |
| [Clients](#clients) | `clients.db` | Client companies, contact persons | Clients (landing page) |
| [Students](#students) | `students.db` | Students of a school (client) | School > Students (and a tab on the Client detail page) |
| [Subjects](#subjects) | `subjects.db` | Subjects and which teacher teaches what | School > Subjects; Subjects tab on the User detail page |
| [Schedule](#schedule) | `schedule.db` | A teacher's recurring weekly lessons | School > Schedule |
| [Grades](#grades) | `grades.db` | Grade scales per country, the grades given to students, and tests with their results | School > Grades, Grade scales, Tests; Grades tab on the Student detail page |
| [Production](#production) | `production.db` | Products, categories, models, units, price history, inventory, bills of materials, work orders, transactions | Production group |
| [Purchasing](#purchasing) | `purchasing.db` | Ship methods, vendors, vendor prices, purchase orders and lines | Purchasing group |
| [Sales](#sales) | `sales.db` | Customers, stores, salespeople, territories, currencies, special offers, tax rates, sales orders and lines | Sales group |
| [Inventory](#inventory) | `inventory.db` | Warehouses, storage locations, stock levels and movements, inventory counts, receiving and issuing against orders | Warehouse group |
| [Users](#users) | `users.db` | Users, sign-in | Users |
| [Addresses](#addresses) | `addresses.db` | Addresses and address types | Address types (Settings) |
| [EmailAddresses](#emailaddresses) | `emailaddresses.db` | Email addresses and types | Email address types (Settings) |
| [PhoneNumbers](#phonenumbers) | `phonenumbers.db` | Phone numbers and types | Phone number types (Settings) |
| [TimeReports](#timereports) | `timereports.db` | Time reports, work time types, exports | Time reports group |
| [Cvs](#cvs) | `cvs.db` | CVs and their sections | CVs |
| [Email](#email) | `email.db` | Mail accounts, messages, drafts | Email group |

**School.** The app is used from a teacher's point of view: a client is a school and a user is a teacher. Students, Subjects, Schedule and Grades are separate projects but one switch in the Modules settings page ("School", `SchoolGroup.ModuleKey`). Switching it off hides the whole School sidebar group and the school tabs on other pages (Students on a client, Subjects on a user); the data is kept. See [architecture.md](architecture.md) for how the tabs find out (`IModuleAvailability`).

The Dashboard (Host) is registered first in `CompositionRoot`, so it is the first page and the one you land on; `AddClientsModule` comes next, before `AddUsersModule`, so Clients is the first module page. See the Dashboard note in [architecture.md](architecture.md).

## Clients

Companies with a unique, normalized organization number. Error codes `client_*`.

**Contact persons.** A client has any number of `ContactPerson` (`ClientId` FK with cascade, first/last name, optional job title and notes; error codes `contact_person_*`), managed in a Contact persons tab on the Client detail page. A person's detail page reuses the Addresses / Email addresses / Phone numbers panel factories with the person's id as `ObjectId`, so those modules need no schema change. Deleting a person publishes `ContactPersonDeleted`; deleting a client publishes one per person before `ClientDeleted`. Addresses, EmailAddresses and PhoneNumbers each consume `ContactPersonDeleted` and remove the person's entries.

Clients implements `IClientLookup` (client names for TimeReports) and `IContactPersonLookup` (recipients for emailed exports), both in `Shared/Clients`.

## Students

The app is used from a teacher's perspective: a **client is a school**, a user is a teacher, and a teacher works at many schools (reports time per client). Each school has any number of students.

- **Student**: `ClientId` (the school), first/last name, optional `ClassName` and `Notes`, and an optional photo. `ClientId` is an id from the Clients module, so there is no foreign key; instead `CreateStudent` asks `IClientLookup.ExistsAsync` (`Shared/Clients`) and fails with `student_school_not_found` for an unknown school (skipped when the Clients module is absent). A student cannot move to another school. Error codes `student_*`.
- **Tab**: Students is a tab on the Client detail page, built through `IClientStudentsPanelFactory` (`Shared/Students`; optional, so the tab disappears if the module is absent).
- **Page**: the module also has its own sidebar page (`ModuleKey` `school`) with the students of every school, a school column and a school filter (names and filter come from `IClientLookup`). The same `StudentsViewModel` serves both: a null `clientId` means the page, where the editor has a school picker for new students.
- **Detail page**: each student opens a detail page with their photo and tabs that reuse the Addresses / Email addresses / Phone numbers panel factories with the student's id as `ObjectId`, e.g. for guardians' contact details.
- **Running notes**: a student has any number of dated `StudentNote` entries (`StudentId` FK with cascade, `Date` chosen by the teacher, `Text` up to 4000 characters), kept in a Notes tab on the detail page (`StudentNotesViewModel`; pick an entry to edit or delete it). `ListStudentNotes` returns the latest date first, ties broken by when the entry was written. Error codes `student_note_*`. This is separate from the single `Notes` field on the student itself, which stays for general information.
- **Photo**: PNG/JPEG up to 6 MB with a 96x96 thumbnail, like users, clients and contact persons (see Cross-cutting: photos). `Get/SetStudentPhoto`, `StudentPhotoPanelFactory`; lists carry only the thumbnail.
- **Lookup**: implements `IStudentLookup` (`Shared/Students`) so other modules (Schedule, Grades) can list the students of a school or of all schools and check that a student exists.
- **Grades tab**: the detail page also shows a Grades tab built through `IStudentGradesPanelFactory` (`Shared/Students`; optional, so the tab disappears if the Grades module is absent).
- **Deleting**: deleting a student publishes `StudentDeleted`. The module consumes `ClientDeleted`, deletes the school's students and publishes one `StudentDeleted` per student; Addresses, EmailAddresses and PhoneNumbers consume `StudentDeleted` and remove the student's entries.

## Subjects

A teacher can teach several subjects, and several teachers can teach the same one.

- **Subject**: a user-managed catalogue entry shared by every teacher (unique case-insensitive `Name`, optional `Description`; nothing is seeded). Own CRUD page in the sidebar (`ModuleKey` `school`). Error codes `subject_*`. Deleting a subject also takes it off every teacher.
- **TeacherSubject**: `UserId` + `SubjectId`, unique per pair. `UserId` is a Users id, so there is no foreign key. Error codes `teacher_subject_*`.
- **UI**: a Subjects tab on the User detail page via `IUserSubjectsPanelFactory` (`Shared/Subjects`; optional, so the tab disappears if the module is absent). It lists what the teacher teaches and offers the catalogue subjects they do not teach yet.
- Consumes `UserDeleted` and removes the user's assignments.
- Publishes `SubjectDeleted` and implements `ISubjectLookup` (`Shared/Subjects`) so other modules can pick a subject and show its name.

## Schedule

A teacher's recurring weekly lessons, for example mathematics every Monday at 08:00.

- **ScheduleEntry**: `UserId` (the signed-in teacher, so each teacher only sees and changes their own lessons), `Day` (`DayOfWeek`), `StartTime` and `EndTime` (`TimeOnly`; the end must be after the start, no lessons past midnight), `SubjectId`, `ClientId` (the school), optional `Room` and `Notes`, and the students who attend (`ScheduleEntryStudent`, entry + `StudentId`). The teacher, subject, school and students live in other modules, so there are no foreign keys to them.
- **Rules**: the subject must exist (`ISubjectLookup`) and the school must exist (`IClientLookup`); the students must belong to that school (`IStudentLookup`, `schedule_student_not_found`); a teacher cannot have two overlapping lessons on the same day (back to back is fine, `schedule_overlap`). Error codes `schedule_*`.
- **UI**: own sidebar page (`ModuleKey` `school`) with the lessons in week order (Monday first), names resolved through the lookups, and an editor where the students of the chosen school are ticked (changing the school clears the ticks). Adding a lesson needs at least one subject and one school.
- Consumes `UserDeleted` (the teacher's lessons), `ClientDeleted` (the school's lessons) `SubjectDeleted` (lessons in that subject) and `StudentDeleted` (takes the student off their lessons; the lessons stay).

## Grades

Grade scales per country, and the grades a teacher gives students. Countries grade differently (A-F in Sweden, 1-6 in Germany, pass/fail elsewhere), so the scales are data the teacher defines rather than something built in.

- **GradeScale**: `Name`, optional `Country` (free text) and `Description`, with its steps (`GradeScaleItem`: `Label` such as "A" or "4", an integer `Rank` where higher is better, optional description and `IsPassing`). A scale needs at least one and at most 50 steps; labels are unique within a scale (case-insensitive). Scales are shared by every teacher. A percentage scale is modelled as bands (for example "90-100").
- **Editing a scale**: the scale and its steps are saved together. Steps keep their id when edited so grades keep pointing at them; a step left out is removed unless a grade uses it (`grade_item_in_use`), and a scale with grades cannot be deleted (`grade_scale_in_use`). Nothing is seeded, so the first scale is created by the teacher.
- **Grade**: `UserId` (the teacher who gave it, so each teacher only sees and changes their own), `StudentId`, `SubjectId`, `ScaleItemId` (the step on a scale, with a restricting FK inside the module), `Date`, optional `Comment`. The student and subject live in other modules, so there are no foreign keys to them; create and update ask `IStudentLookup` and `ISubjectLookup` (`grade_student_not_found`, `grade_subject_not_found`). A grade stays with its student. Lists show the latest date first.
- **UI**: two pages in the School group, both under `ModuleKey` `school`: Grades (every student's grades, with a student picker in the editor) and Grade scales (a scale editor with an editable list of steps). The same `GradesViewModel` also serves the Grades tab on the Student detail page (`StudentGradesPanelFactory`), where the student is fixed. Error codes `grade_*`.
- **Test**: a test the teacher gave: `UserId` (owner, so each teacher only sees their own), `Name` (≤100), `Date` (the day it was taken) and `MaxScore` (a number above 0, at most 100000). The maximum cannot be lowered below a score already given (`test_max_score_below_results`).
- **TestResult**: one student's result on a test: `StudentId` (other module, so no foreign key; checked through `IStudentLookup`), `Score` (0 up to the test's maximum) and an optional `ScaleItemId` (the grade, a step on any scale, with a restricting FK inside the module so a scale step in use cannot be removed and a scale in use cannot be deleted). A student has at most one result per test: saving again replaces it. Deleting a test deletes its results (cascade). Scores are typed as text and accept a decimal point or comma (`ScoreText`).
- **Tests page**: a third page in the School group (`TestsPage`, `ModuleKey` `school`): the test list, an editor for name/date/maximum score and, for a saved test, the results panel (student picker, score, scale and grade). Error codes `test_*`; a missing grade step reuses `grade_item_not_found`.
- Consumes `UserDeleted` (the grades and tests that teacher gave), `StudentDeleted` (their grades and test results) and `SubjectDeleted`.

## Production

Products and what goes into making them, modelled on the AdventureWorks sample's Production schema. One sidebar group "Production" (`ModuleKey` `production`) with one page per entity. Error codes are the entity name in snake case (`product_*`, `work_order_*`, ...).

- **Catalogue**: `ProductCategory`, `ProductSubcategory` (of a category), `ProductModel`, `UnitMeasure` (seeded with each, kilogram, gram, meter, centimeter, liter, hour, package) and `Product` (name and product number unique, make/salable flags, color, size and weight with units, stock levels, standard cost, list price, product line/class/style, subcategory, model, sell dates; a sell end before the start is `product_end_before_start`).
- **History and stock**: `ProductCostHistory` and `ProductListPriceHistory` (dated, deleted with the product), `ProductInventory` (quantity per product and location, unique per pair) and `Location` (cost rate, available hours; four seeded). `ProductReview` (rating 1-5) is deleted with its product.
- **Making things**: `BillOfMaterial` (assembly product, component product, unit, level, quantity per assembly), `WorkOrder` (number handed out as `WO-000001` when left blank; product, quantities, dates, optional `ScrapReason`, seeded with a few reasons), `WorkOrderRouting` (steps of a work order at a location, unique per sequence number) and `TransactionHistory` (work order, sales order or purchase order transactions).
- **Rules**: rows that others point at cannot be deleted (`*_in_use`), except children that go with their parent (history, reviews, inventory, routing steps). Deleting a product publishes `ProductDeleted`.
- **Not ported**: documents, illustrations, product photos and the culture-specific descriptions of the sample, and the archive table of transactions.
- **Contracts**: implements `IProductLookup` and `IUnitMeasureLookup` (`Shared/Products`) for Purchasing and Sales.
- **Product page**: the Products list has an Open button that shows `ProductDetailViewModel` with tabs Information, Cost history, List prices, Inventory, Reviews and Bill of materials (the product as assembly). The tabs are the ordinary list pages limited to the product (see "Detail pages and order screens" below).

## Purchasing

Vendors and what is bought from them. Sidebar group "Purchasing" (`ModuleKey` `purchasing`).

- **ShipMethod** (name, base charge, rate per unit; three seeded) implements `IShipMethodLookup` (`Shared/Purchasing`), which Sales uses too.
- **Vendor**: account number and name unique, credit rating 1-5, preferred and active flags, optional web address and an optional link to a client (by id, no foreign key; cleared when the client is deleted).
- **ProductVendor**: what a vendor charges for a product (lead time, standard and last receipt price, order quantities, unit). Unique per product and vendor. `ProductId` and the unit are ids from Production, so there is no foreign key; they are checked through `IProductLookup` / `IUnitMeasureLookup` (skipped if Production is absent). Consumes `ProductDeleted` and removes the prices of that product.
- **PurchaseOrder** and **PurchaseOrderDetail**: an order has a number (`PO-000001` when left blank), status, vendor, ship method, optional buyer (a user id, cleared when the user is deleted), dates, tax and freight; its subtotal and total due are kept by `OrderTotals` from the lines whenever a line or the order changes. A line's total is quantity times price and its stocked quantity is received minus rejected. Lines are deleted with the order and keep their product id if the product is deleted (shown without a name).
- **Vendor page**: Open on a vendor shows tabs Information, Prices, Purchase orders, and Addresses, Email addresses and Phone numbers when those modules are present (the vendor id is the object id).
- **Order screen**: Open on a purchase order shows `PurchaseOrderScreenViewModel`: the order's editor on the left, a totals strip (subtotal, tax, freight, total due) and the lines on the right. Adding, changing or deleting a line refreshes the totals; saving the header does too. The separate line page stays for searching all lines.

## Inventory

The warehouse: where goods are stored and how many there are. Sidebar group "Warehouse" (`ModuleKey` `inventory`). The module is named Inventory because an entity called `Warehouse` would clash with a module namespace of the same name. It owns its stock; Production's `ProductInventory` is untouched.

- **Warehouse** (code and name unique, optional address, active flag; `MAIN` is seeded) and **StorageLocation** (a bin or shelf: unique code, optional zone, description and capacity), deleted with its warehouse. A warehouse with movements at any of its locations cannot be deleted (`warehouse_in_use`). Open on either shows a detail page: a warehouse has tabs Information, Locations and Counts; a location has Information, Contents and Movements.
- **StockMovement** is the ledger: date, product, location, type (Receipt, Return, TransferIn and AdjustmentUp add; Issue, Scrap, TransferOut and AdjustmentDown take away), quantity above zero, reference and note. `StockLedger` keeps **StockLevel** (one row per product and location, optional minimum) equal to the signed sum of the movements, recalculated whenever a movement is created, changed or deleted, and refuses any create, change or delete that would leave a location below zero (`stock_movement_insufficient_stock`), including deleting or shrinking a receipt that later issues depend on. Each of these, a transfer, a goods movement and the posting of a count runs in one transaction, so a refused or failed change leaves nothing behind. A stock level can only have its minimum edited (`stock_level_key_fixed`); its quantity follows the movements. Sums are done client-side because SQLite cannot sum decimals.
- **Stock overview** (read-only page): products by warehouse and location with quantity and minimum, a search, a warehouse filter and an "only low stock" filter; at most 500 rows. The filters run in the database and only the first products by name are read in full.
- **Stock transfer** moves a quantity between two locations as a TransferOut and TransferIn pair; **Receive goods** and **Issue goods** pick an order from the order modules, show ordered, already handled and outstanding quantities, and book Receipt or Issue movements with the order number as reference. An issue is checked for every line before anything is saved, and the quantity cannot go past what the order has outstanding (`goods_movement_exceeds_order`), so a repeated submit books nothing. Order modules are optional; without them the pages say so.
- **InventoryCount** (number `IC-000001` when left blank, warehouse, date, note, posted flag) and **InventoryCountLine** (product, location, expected and counted quantity, difference). Open shows one screen: Fill adds a line per non-zero stock level of the warehouse, the counted quantities are entered, and Post records an AdjustmentUp or AdjustmentDown for each counted line by counted minus current stock. A posted count cannot be filled or posted again.
- **Contracts**: consumes `ProductDeleted` (stock levels and the lines of unposted counts are removed; movements and posted count lines keep the id and the history). Uses `IPurchaseOrderLookup` and `ISalesOrderLookup` (both `IOrderLookup`, `Shared/Orders`) and publishes `GoodsReceived` or `GoodsIssued` with the running total for each line (a movement remembers its order line in `SourceLine`, and the cap on how much can be handled is per line). The totals are absolute, so a repeat does no harm, and opening an order on the receive or issue page resends them, which brings an order module back in step after a failed delivery. Purchasing sets the line's received and stocked quantities and refreshes the order totals; Sales sets the line's shipped quantity (`ShippedQty`) and marks the order Shipped with a ship date once every line is out, unless it is cancelled or rejected.

## Sales

Customers and what is sold to them. Sidebar group "Sales" (`ModuleKey` `sales`).

- **Territories and people**: `SalesTerritory`, `SalesPerson` (a name, an optional user link, territory, quota, bonus, commission 0-1), `SalesPersonQuotaHistory` and `SalesTerritoryHistory` (deleted with the salesperson).
- **Money**: `Currency` (seeded SEK, USD, EUR, GBP, NOK, DKK, CNY), `CurrencyRate` (unique per date and currency pair), `CountryRegionCurrency` (country as free text) and `SalesTaxRate` (percent).
- **Offers**: `SpecialOffer` (discount 0-1, dates, quantities; "No discount" seeded) and `SpecialOfferProduct`, which consumes `ProductDeleted`.
- **Customers**: `Store`, and `Customer` (number `CU-000001` when left blank, name, optional link to a client, store and territory). Deleting a client clears the link, the customer stays.
- **SalesOrder** and **SalesOrderDetail**: number `SO-000001` when left blank, status, dates, customer, optional salesperson, territory and ship method (`IShipMethodLookup`), tax, freight and comment. A line's total is price times quantity less the discount; the order's subtotal and total due follow the lines (`OrderTotals`). `SalesOrderReason` ties sales reasons (`SalesReason`, seeded) to an order.
- **Not ported**: credit cards (card data is a security risk), shopping carts, and the customer's and order's addresses.
- **Customer page**: tabs Information, Sales orders and the contact tabs (as for vendors). **Salesperson page**: tabs Information, Quota history and Territory history. **Order screen**: as for purchase orders (`SalesOrderScreenViewModel`).

### Detail pages and order screens

The three modules share one pattern, produced by the same generator as the list pages. A child entity declares its parent (`ProductCostHistory` -> `Product`); its list handler takes a `ParentId`, and its page view model has `ForParent(parentId)`, which limits the list to that parent, gives new rows the parent (the editor hides the parent drop-down, `IsParentFixed`), drops the page's title and margin (`IsEmbedded`) and raises `Changed` after a save or delete. An entity with a detail page has an Open button and `Detail` on its list page; the detail view model derives from `DetailViewModel` (Back raises `Closed`) and is created by `IDetailFactory<TDto>` with the container supplying everything but the row. The detail view models and their views are hand-written next to the generated files; the view is found by name (`*ViewModel` -> `*View`) like every other page.

## Users

CRUD plus authentication (see [architecture.md](architecture.md#startup-and-authentication)). Passwords are hashed with `IPasswordHasher<T>`; plain text is never stored or logged. Error codes `user_*`. Implements `IAuthenticationPages` (login and first-user setup) and `IUserLookup` / `IUserPhotoLookup` (`Shared/Users`).

## Addresses

Owns addresses for every other module. An `Address` points at its owner by a single `ObjectId`; user, client and contact person ids are globally unique GUIDs, so no owner type is stored. Listing is always per `ObjectId`; `DeleteAddressesForObject` removes all of an owner's addresses.

- **AddressType**: user-managed lookup (unique case-insensitive `Name`, optional `Description`). `Billing`, `Delivery`, `Visiting` and `Home` are seeded once by the migration via `HasData`, then editable and deletable. A type in use cannot be deleted (`address_type_in_use`).
- **Address**: `ObjectId`, `AddressTypeId`, `CareOf`, `Street1/2`, `PostalCode`, `City`, `State`, `Country`.
- Other modules show an object's addresses through `IObjectAddressesPanelFactory` (`Shared/Addresses`). The Users and Clients detail pages each build an Addresses tab from `factory.Create(id)`; the factory is optional, so the tab disappears if the module is absent.
- Consumes `UserDeleted`, `ClientDeleted` and `ContactPersonDeleted`.

## EmailAddresses

The email counterpart of Addresses, same design: user-managed `EmailAddressType` (seeded `Work`, `Private`, `Billing`), `EmailAddress` (`ObjectId`, type, `Email`; unique per object, case-insensitive), `IObjectEmailAddressesPanelFactory`, and the same three delete consumers. Error codes `email_address_*` / `email_address_type_*`.

## PhoneNumbers

Same design again: user-managed `PhoneNumberType` (seeded `Mobile`, `Work`, `Home`), `PhoneNumber` (`ObjectId`, type, `Number` as typed plus a `NormalizedNumber` of an optional `+` and digits; 5-15 digits; unique per object by normalized value, so `070-123 45 67` and `0701234567` clash), `IObjectPhoneNumbersPanelFactory`, and the same three delete consumers. Error codes `phone_number_*` / `phone_number_type_*`.

## TimeReports

Lets the signed-in user report time per client.

- **TimeReport**: `UserId`, `ClientId`, `Date`, `StartTime`/`EndTime`, `TotalHours` (typed by hand, not derived from start/end), optional `Location`, `DrivingKilometers`, and a `WorkTimeTypeId`.
- **Rules**: end after start (no overnight), hours 0-24, km 0-10000. Error codes `time_report_*`.
- **Per user**: every handler is scoped by `UserId`, taken from `ICurrentUser` (`Shared/Auth`), which the Host sets on sign-in and clears on sign-out.
- **Client names** come from `IClientLookup`, so the module never references Clients. Reports are deliberately kept when a client or user is deleted (billing records); the list then shows no client name.
- **WorkTimeType**: user-managed lookup in the same database (unique case-insensitive `Name`, optional `Description`; `Work`, `Sick`, `Vacation`, `Parental leave` seeded via `HasData`, then editable and deletable) with its own CRUD page. A type in use cannot be deleted (`work_time_type_in_use`). Error codes `work_time_type_*`.
- **UI**: a sidebar page (client filter, totals, 15 reports per page with Previous/Next; Copy and Delete sit in a toolbar above the table) and a Time reports tab on the Client detail page via `ITimeReportsPanelFactory` (`Shared/TimeReports`). TimeReports and WorkTimeTypes share the "Time reports" sidebar group.
- **Excel export** (ClosedXML, `ExportTimeReports`): one client and one calendar year, one sheet per month named `yyyy-MM`. `ExportKilometerSummary` gives one sheet for a year with driving km per client (rows) and month (columns), row/column totals and the year total, for tax purposes. Files are handed to the user through `IFileSaveService` (`Shared/Files`, dialog in the Host).
- **Email export** (`SendTimeReportViewModel`, shown in the editor slot): the recipient is the client or one of its contact persons (`IContactPersonLookup`). The address is chosen among the email addresses registered for that recipient, read through every `IContactInfoSource` of kind Email, and picked automatically when there is only one. The mail goes out through `ISystemMailSender` (`Shared/Email`, implemented by Email), which always uses the system SMTP account (first by name; error `email_no_system_smtp` if none). The Email button is hidden when the Email module is absent.

## Cvs

Each user keeps several CVs aimed at different jobs: `Cv` (`UserId`, unique case-insensitive `Title` per user, optional `TargetJob`, `Summary`). Handlers are scoped by `UserId`. Error codes `cv_*`.

- **UI**: its own sidebar page (signed-in user) and a CVs tab on the User detail page via `IUserCvsPanelFactory` (`Shared/Cvs`). Each CV opens a read-only detail page (owner details from `IUserLookup`, photo, summary, work experience, education, courses, languages, driver licence); entries are changed in an editor that replaces the overview.
- **Photo**: stored per CV (PNG/JPEG, max 6 MB), picked through `IFileOpenService` (`Shared/Files`, dialog in the Host).
- **Contact details** are the user's own data from Addresses, PhoneNumbers and EmailAddresses, read through `IContactInfoSource` (`Shared/Contacts`, one implementation per module, so Cvs never references them). Each CV stores only which entry ids to show (`CvContactSelection`: ids of other modules, so no foreign key; `SetCvContacts` ignores ids that are not the user's; deleted ones just disappear), plus an optional `LinkedIn` URL (normalized to https, must be on linkedin.com, error `cv_linkedin_invalid`).
- **Word export** (`ExportCvToWord`, Open XML SDK) in the current UI language, handed to the user through `IFileSaveService`.
- Consumes `UserDeleted` and deletes the user's CVs.

## Email

Holds the mail accounts and uses them to send and receive mail. Sidebar group "Email" with Inbox, New message, Drafts, Sent mail and Email accounts pages (`ModuleKey` `email`).

**Accounts.** `EmailAccount` is either SMTP or POP3 (`Protocol`), with `Host`, `Port`, `Security` (None / StartTls / SslOnConnect), `EmailAddress`, optional `Username` and a password stored only as `ProtectedPassword` (Windows DPAPI through `ISecretProtector`, so the module only runs on Windows; handlers and DTOs never return it, and an update with a null password keeps the stored one). A stored password is reused (test connection, update with an empty password) only while host, port and username are unchanged, otherwise `email_account_password_required`.

An account is personal (`OwnerId` = the user) or a system account shared by everyone (`OwnerId` = `EmailAccount.SystemOwner`, an empty Guid, so the unique case-insensitive `(OwnerId, Name)` index covers it). A user sees and may change their own and the system accounts, never another user's. There are no roles, so any signed-in user can add, change or delete system accounts, and the scope cannot be changed after creation. `UserDeleted` deletes the user's personal accounts only. Error codes `email_account_*`.

**Server access.** Everything goes through `IMailGateway` (`Mail/`, the only MailKit user), which sorts failures into `email_authentication_failed` / `email_tls_failed` / `email_connection_failed` / `email_server_error`; tests use a fake. `TestEmailConnection` tries the editor's settings.

**Receiving.** `FetchEmail` downloads up to 50 of the newest new messages of a POP3 account into `ReceivedMessage` (unique per account + UIDL, body cut at 100 000 characters, plain text kept, HTML kept separately) and never deletes on the server. Messages belong to the account, so a system inbox is shared, and they go with the account (cascade). UIDLs longer than 200 characters are stored as `sha256:` + hash (`MailLimits.NormalizeUid`). Deleting a received message is local only: its UIDL goes into `DeletedMessage` so later fetches skip it. The inbox lists 50 messages and loads older ones on request. The inbox view runs a `DispatcherTimer` that calls `AutoRefreshCommand` every 5 minutes; the view model decides whether to fetch (checkbox, not busy), stays quiet unless something new arrived or it failed, and keeps the open message.

**Sending.** `SendEmail` sends text or HTML from the account's address through an SMTP account the user may use, with attachments (`IFilePickService` in `Shared/Files`, dialog in the Host; unreadable or too large files come back as `Skipped` so the composer can say so). It keeps a `SentMessage` copy after the server accepted the mail; failing to keep the copy is ignored so a retry does not send twice. Sent copies can be deleted. Messages carry `To`/`Cc`; `SendEmail` removes Cc addresses already in To and limits To plus Cc to 50.

**Attachments** (`MailAttachment`, bytes in the database) belong to a received or sent message and are saved through `IFileSaveService`. Limits: 20 per message, 10 MB each, 25 MB together when sending; larger received ones are skipped.

**Compose.** Reply, Reply all and Forward open an embedded `ComposeViewModel` in the inbox (forward keeps attachments). Reply all sends to the sender plus the other To recipients with the original Cc as Cc, leaving out our own address and repeats; mail fetched before recipients were stored replies to the sender only.

**Drafts** (`Draft`, `SaveDraft` / `ListDrafts` / `GetDraft` / `DeleteDraft`, error codes `email_draft_*`) are personal to the user even when written for a system account, keep their attachments (replaced inside one transaction), are deleted when sent and with the user. The composer has a Save draft button.

**HTML.** Received HTML is stored (`HtmlBody`, cut at 200 000 characters) and shown by `HtmlFormatter` (MimeKit tokenizer to blocks of styled runs, drawn by `FormattedBodyPanel`): paragraphs, headings, lists, quotes, bold/italic/underline. Scripts, styles, forms and frames are dropped, images are never loaded (shown as their alt text), and links are drawn but not clickable (address in a tooltip). The inbox offers a Show plain text toggle.

`SystemMailSender` implements `ISystemMailSender` for other modules.

## Cross-cutting: photos

Users, clients, contact persons and students can have a photo (PNG/JPEG, max 6 MB), stored as `Photo` bytes in the owning module's database. Cvs keeps its own per-CV photo.

- `Get<X>Photo` / `Set<X>Photo` handlers read and write only that column, so lists and updates never load it.
- Shared pieces in `Shared/Photos`: `PhotoRules`, `PhotoThumbnail`, `BytesToBitmapConverter`, `PhotoPanelViewModel` / `PhotoPanelView` (round photo or initials with Change/Remove buttons on the User and Client detail pages) and `AvatarView`. Each module hands the panel its load/save delegates through its `<X>PhotoPanelFactory`; the file dialog is `IFileOpenService`.
- Error codes `photo_too_large` / `photo_invalid` (shared strings).
- Every saved photo also gets a 96x96 PNG `PhotoThumbnail` (SkiaSharp, made by the `Set<X>Photo` handler; bytes that cannot be decoded are `photo_invalid`). List queries and DTOs carry only the thumbnail (`Thumbnail`), shown by `AvatarView` in the Users, Clients and Contact persons lists, the sidebar and the top bar. Photos saved before thumbnails existed show initials in lists until they are uploaded again.
- The signed-in user's picture reaches the shell through `IUserPhotoLookup` (implemented by Users). `SetUserPhoto` publishes `UserPhotoChanged`, which the Host's `UserPhotoNotifier` turns into a refresh of the open shell.
