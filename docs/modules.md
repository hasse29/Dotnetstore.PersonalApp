# Modules

Each module is one project under `src/Modules/<Module>/` with its own SQLite database in `%LOCALAPPDATA%/Dotnetstore.PersonalApp`. Modules never reference each other; they share only contracts in `Shared` (see [architecture.md](architecture.md)). Error codes are prefixed per module, so resource keys `Error_{code}` never collide.

| Module | Database | Owns | Sidebar |
| --- | --- | --- | --- |
| [Clients](#clients) | `clients.db` | Client companies, contact persons | Clients (landing page) |
| [Students](#students) | `students.db` | Students of a school (client) | School > Students (and a tab on the Client detail page) |
| [Subjects](#subjects) | `subjects.db` | Subjects and which teacher teaches what | School > Subjects; Subjects tab on the User detail page |
| [Schedule](#schedule) | `schedule.db` | A teacher's recurring weekly lessons | School > Schedule |
| [Grades](#grades) | `grades.db` | Grade scales per country, the grades given to students, and tests with their results | School > Grades, Grade scales, Tests; Grades tab on the Student detail page |
| [Users](#users) | `users.db` | Users, sign-in | Users |
| [Audit](#audit) | `audit.db` | The audit log: who did what to which entity, when, with what outcome | Audit log |
| [Contact](#contact) | `contact.db` | Addresses, email addresses and phone numbers, each with user-managed types | Address types, Email address types, Phone number types (Settings) |
| [TimeReports](#timereports) | `timereports.db` | Time reports, work time types, exports | Time reports group |
| [Products](#products) | `products.db` | Hardware, software and consulting products, categories, suppliers, supplier prices and stock (locations, movements, inventory counts) | Products group |
| [Sales](#sales) | `salesdocuments.db` | Customers, offers, orders, pick lists (reduce stock), invoices, payments and credit | Sales group |
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
- **Deleting**: deleting a student publishes `StudentDeleted`. The module consumes `ClientDeleted`, deletes the school's students and publishes one `StudentDeleted` per student; Contact consumes `StudentDeleted` and remove the student's entries.

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

## Users

CRUD plus authentication (see [architecture.md](architecture.md#startup-and-authentication)). Passwords are hashed with `IPasswordHasher<T>`; plain text is never stored or logged. Error codes `user_*`. Implements `IAuthenticationPages` (login and first-user setup) and `IUserLookup` / `IUserPhotoLookup` (`Shared/Users`).

## Contact

One module owns the contact details of every other module: addresses, email addresses and phone numbers, in one project (`Addresses/`, `EmailAddresses/`, `PhoneNumbers/` folders, namespaces `Modules.Contact.<Area>`) with one `ContactDbContext` and one database, `contact.db`. It replaces the former Addresses, EmailAddresses and PhoneNumbers modules: at startup `MigrateContactDatabaseTask` copies the rows of their old `addresses.db`, `emailaddresses.db` and `phonenumbers.db` into `contact.db` (seeded types keep their ids, so they are not duplicated) and deletes the old files. Types are matched by id, then by name: a legacy type whose name already exists in `contact.db` (a seeded type the user deleted and re-created) is merged into the existing one and its rows are pointed at it. A legacy file that cannot be imported (damaged, locked) is left untouched and retried at the next start; it never blocks startup or the other files. Restoring a backup made before the merge brings the old files back and they are imported the same way at the next start, so rows already in `contact.db` win and rows deleted since can reappear. The three settings pages share the module key `contact`, and one `OwnerDeletedConsumer` handles `UserDeleted`, `ClientDeleted`, `ContactPersonDeleted`, `CustomerDeleted` and `StudentDeleted` for all three areas. `IContactInfoSource.Kind` tells a caller which kind a source returns, and address entries carry their `Lines`, so Sales prints an address without parsing the one-line text.

### Addresses

Owns addresses for every other module. An `Address` points at its owner by a single `ObjectId`; user, client and contact person ids are globally unique GUIDs, so no owner type is stored. Listing is always per `ObjectId`; `DeleteAddressesForObject` removes all of an owner's addresses.

- **AddressType**: user-managed lookup (unique case-insensitive `Name`, optional `Description`). `Billing`, `Delivery`, `Visiting` and `Home` are seeded once by the migration via `HasData`, then editable and deletable. A type in use cannot be deleted (`address_type_in_use`).
- **Address**: `ObjectId`, `AddressTypeId`, `CareOf`, `Street1/2`, `PostalCode`, `City`, `State`, `Country`.
- Other modules show an object's addresses through `IObjectAddressesPanelFactory` (`Shared/Addresses`). The Users and Clients detail pages each build an Addresses tab from `factory.Create(id)`; the factory is optional, so the tab disappears if the module is absent.
- Consumes `UserDeleted`, `ClientDeleted`, `ContactPersonDeleted`, `CustomerDeleted` and `StudentDeleted`.

### EmailAddresses

The email counterpart of Addresses, same design: user-managed `EmailAddressType` (seeded `Work`, `Private`, `Billing`), `EmailAddress` (`ObjectId`, type, `Email`; unique per object, case-insensitive), `IObjectEmailAddressesPanelFactory`, and the same three delete consumers. Error codes `email_address_*` / `email_address_type_*`.

### PhoneNumbers

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
- **Expense**: `UserId`, optional `ClientId`, `Date`, `Category` (fixed enum `ExpenseCategory`: Hotel, Travel, Meals, Parking, Fuel, Materials, Other; stored by name), `Description`, `Amount` (paid, VAT included) and `VatAmount` (part of the amount). Like time reports, expenses are per user and are kept when a client or user is deleted, since they are tax records. Error codes `expense_*`.
- **Receipt**: `ExpenseReceipt` holds one PDF/PNG/JPEG (max 10 MB, checked by extension) per expense in its own table, so listing never loads the bytes; it is replaced or removed through `UpdateExpense` and deleted with the expense (cascade). The editor attaches through `IFilePickService` and saves a copy back out through `IFileSaveService` (`GetExpenseReceipt`).
- **Expenses UI and export**: an Expenses page in the "Time reports" sidebar group (15 per page) with a year picker and an Export to Excel button (`ExportExpenses`): one sheet listing every expense of the calendar year (date, category, description, client, amount, VAT, receipt file name) with totals, and a Summary sheet with amount and VAT per category. SQLite cannot order by `DateTimeOffset`, so lists tie-break on `Id` (a v7 Guid).

## Products

The catalogue of what is sold: hardware (cables, tools, computer parts), software the user makes, and consulting hours. A single `Product` table with a `Kind` enum (`ProductKind`: Hardware, Software, Consulting).

- **Product**: unique case-insensitive `Number` (SKU, max 30), `Name`, `Kind`, optional `Description`, `IsActive`, `Unit` (`ProductUnit`: Piece, Hour, Day, Month, License), `UnitPrice`, `CostPrice`, `VatRate` (default 25), optional category.
- **Kind-specific details**: `Manufacturer`/`Model` (Hardware only), `SoftwareVersion` (Software only), `MinimumHours` (Consulting only). `ProductRules.Apply` clears the fields that do not belong to the chosen kind, and the editor only shows the matching ones. A new product gets a default unit per kind (Piece, License, Hour).
- **ProductCategory**: user-managed lookup in the same database (unique case-insensitive `Name`, optional `Description`), its own page in the Products sidebar group. A category used by products cannot be deleted (`product_category_in_use`; the FK is `Restrict`).
- **Rules**: prices 0-10,000,000 (rounded to 2 decimals), VAT 0-100, minimum hours more than 0 and at most 1000. Error codes `product_*` and `product_category_*`.
- **UI**: a list with search (number, name, manufacturer, model), a kind filter, 15 products per page, a thumbnail per row and an editor beside the list.
- **Detail page**: the Details button opens `ProductDetailViewModel` in place of the list: photo, kind, status, category, the kind-specific details, and a pricing card (price, price incl. VAT, cost, VAT, margin as profit and percent of the price). Edit returns to the list with the product open in the editor.
- **Photo**: PNG/JPEG up to 6 MB with a 96x96 thumbnail, like students (see Cross-cutting: photos). `Get/SetProductPhoto`, `ProductPhotoPanelFactory`; `ListProducts` projects column by column so full-size photos are never loaded for a list.
- **Supplier**: who the products are bought from, in the same module and database so prices can point at products with real foreign keys. Unique case-insensitive `Name`, optional organization number, email (loose format check), phone, website and notes, and `IsActive` (inactive suppliers keep their prices but are not offered for new ones). Own page in the Products sidebar group. Error codes `supplier_*`.
- **SupplierPrice**: one supplier's purchase price (excluding VAT, per the product's unit) for one product, with the supplier's own article number and when it was last changed. Unique per supplier and product, so `SetSupplierPrice` adds or replaces. The FKs cascade: deleting a supplier or a product removes its prices.
- **Stock**: lives in the Products module so quantities point at products and locations with real foreign keys. Consulting is not stockable (`product_not_stockable`); `Product.MinimumStock` (0-1,000,000, non-consulting only) marks a product as low when the total falls below it.
- **StorageLocation**: unique case-insensitive `Name`, optional description, `IsActive` (inactive locations keep their stock but take no new goods). A location with stock levels or movements cannot be deleted (`storage_location_in_use`; make it inactive).
- **StockLevel / StockMovement**: `StockLedger.ApplyAsync` is the only place quantities change. It updates the level for (product, location) and appends an immutable movement (Receipt, Issue, TransferIn, TransferOut, CountAdjustment) in the same `SaveChanges`. Issues and transfers cannot go below zero (`stock_insufficient`); a transfer writes two movements sharing a `TransferId`. Receipts may name the supplier. A product with movements cannot be deleted (`product_in_use`).
- **InventoryCount**: a count for one location (or all, with an option to include products with no stock). Lines snapshot the expected quantity; counted quantities are saved as you go. Posting compares with the stock at that moment and writes `CountAdjustment` movements for the differences, then locks the count. Posted counts cannot be edited (`inventory_count_posted`).
- **Stock UI**: Stock, Stock movements, Inventory counts and Storage locations pages in the "Stock" sidebar group, plus a stock card on the product detail page.
- **Prices on the detail page**: a Suppliers card (`SupplierPricesViewModel`) lists every supplier's price cheapest first, marks the lowest one when there is more than one, and lets you add, change and remove prices.
- **Product catalog**: `IProductCatalog` (Shared) lets Sales search active products for offer lines. Nothing else reads products or suppliers.
- **Stock service**: `IStockService` (Shared) lets Sales issue goods when picking and return them; see Sales.

## Sales

Owns `salesdocuments.db` (not `sales.db`: that file belonged to the old Sales module and is still deleted at startup). The flow is offer, then order and pick list, then invoice.

- **Customer**: someone the owner sells to, a company or a private person. Unique case-insensitive `Name`, optional organization number, notes and `IsActive`. Email addresses, phone numbers and addresses are not stored on the customer: they live in the Addresses, EmailAddresses and PhoneNumbers modules and are managed on the detail page. Customers are the module's own and are not linked to Clients. A customer with offers cannot be deleted (`customer_in_use`; make it inactive). Error codes `customer_*`.
- **Customer detail page**: a Details button on the customers list opens a page with the customer's photo and Information, Addresses, Email addresses and Phone numbers tabs. The tabs reuse the Addresses / EmailAddresses / PhoneNumbers panel factories with the customer's id as `ObjectId`, so those modules need no schema change. An offer copies the customer's first address (by address type name, read through `IContactInfoSource`, one line per part) when it is created, and a draft offer refreshes it each time it is saved. Deleting a customer publishes `CustomerDeleted`, which the three modules consume to remove its entries. Photo: PNG/JPEG up to 6 MB with a 96x96 thumbnail like Clients (`Get/SetCustomerPhoto`, `CustomerPhotoPanelFactory`; see Cross-cutting: photos).
- **Offer**: a number (`O-<year>-<sequence>`, one more than the highest of the year so a deleted draft never makes a number repeat), a customer, a date, a valid-until date, an optional reference and notes, a status and lines. The customer's name and address are copied onto the offer, so editing or renaming a customer never changes an issued offer. Error codes `offer_*`.
- **Offer line**: a description, quantity, unit key (Piece, Hour, Day, Month, License), unit price excluding VAT, VAT percent and discount percent. A line made from a product copies its description, price, unit and VAT through `IProductCatalog` (Shared, implemented by Products); `ProductId` is a plain value, not a foreign key, since the product lives in another database. Free-text lines have no product.
- **Totals**: `OfferTotals` rounds each line's net amount (quantity x price less discount) and its VAT to 2 decimals and adds them up, as an invoice does. The same code runs in the editor and in the handlers; totals are never stored.
- **Status**: Draft, Sent, Accepted, Declined. Draft to Sent needs at least one line; Sent to Accepted, Declined or back to Draft; Declined back to Draft. Accepted is final: it is what the order will be made from. "Expired" is only shown, for a Sent offer past its valid-until date. Only a Draft can be edited (`offer_not_editable`); only a Draft or Declined offer can be deleted (`offer_not_deletable`).
- **Order**: made from an accepted offer with a button on the offer (`CreateOrderFromOffer`). Number `ORD-<year>-<sequence>`; the offer number, customer name and address, reference, notes and all lines are copied, so the order stays as agreed. An offer has one live order at a time (`order_already_exists`); cancelling an order lets the offer be ordered again. Status: Open, ReadyToInvoice (everything picked, or nothing to pick), Invoiced and Cancelled. An invoiced order cannot be cancelled until its invoice is (`order_invoiced`). Error codes `order_*`.
- **Pick list**: made together with the order for its physical lines. A line is physical when it was made from a stockable product (`ProductSummary.IsStockable`, false for consulting); free-text lines and consulting hours are not picked. Number `PL-<year>-<sequence>`, one per order. Lines record how much has been picked, never more than ordered (`picklist_picked_invalid`). When every line is fully picked the list is Complete and the order ReadyToInvoice; picking less again reopens both. Cancelling the order cancels the list and locks it; an invoiced order locks it too (`picklist_locked`).
- **Stock**: picking takes goods out of stock through `IStockService` (Shared, implemented by Products). Products chooses the locations, the ones with the most of the product first, and Sales records the split in `PickAllocation` rows on the line. Picking less, or cancelling the order, puts the goods back at the locations they came from. Not enough in total fails with `stock_insufficient_total` and changes nothing. Stock and Sales are separate databases, so there is no shared transaction: every line is saved right after its stock change, so a failure part-way leaves the finished lines consistent. Only a crash between the two saves can leave them out of step.
- **Invoice**: made from a ReadyToInvoice order with a button on the order, with payment terms in days (default 30, 0 to 365: `invoice_payment_days_invalid`). Number `INV-<year>-<sequence>`, never reused; customer, reference, notes and lines are copied from the order. The order becomes Invoiced. Status is kept in step with the payments: Unpaid (nothing paid), PartiallyPaid, Paid (paid in full) and Cancelled. "Overdue" is only shown, for an Unpaid or PartiallyPaid invoice past its due date. An invoice can only be cancelled while it has no payments (`invoice_has_payments`); cancelling it makes the order ReadyToInvoice again so it can be invoiced anew. Invoices cannot be deleted. Error codes `invoice_*`.
- **Payment**: any number of payments on an invoice, so a customer can pay in parts, in one go, or too much. `RecordPayment` takes an amount (more than 0, at most 10 000 000, rounded to 2 decimals), an optional date (default today, not before the invoice date) and a note. As much as the invoice still needs goes to it (`Payment.Amount`); anything more is `Payment.Excess` and becomes credit. A payment on an already paid invoice is all credit. A mistaken payment can be deleted (`DeletePayment`); that takes back its credit, which fails with `credit_insufficient` if the credit was already used or paid back. Paid is always the sum of the payments, never stored. Error codes `payment_*`.
- **Credit**: what a customer has paid over, as a signed ledger of `CreditEntry` rows (Overpayment adds; Applied and Refund take away); the balance is their sum and never goes below zero. The Credits page lists customers with a balance and their ledger. Credit is used with `ApplyCredit` on another invoice of the same customer (at most the balance and at most what the invoice still needs; the button on the invoice screen uses as much as fits), or paid back with `RefundCredit`, which only records that the money was returned outside the app. Credit belongs to the customer, not to the invoice, so cancelling or reopening invoices never moves it. Error codes `credit_*`.
- **Orders UI**: Orders (read-only, with Cancel) and Pick lists (enter what was picked, Pick everything) pages in the Sales group; the pick list filter starts on Open. A ready order gets payment terms and a Create invoice button. Invoices page: list with search, a status filter and what is left to pay, and an invoice screen with its payments, a Record payment form (amount, date, note), Use credit and Cancel invoice. Paying too much is answered on the spot with how much became credit and what to do with it. Credits page: customers with credit, the ledger and a Record refund form.
- **Offer UI**: Offers and Customers pages in the Sales sidebar group. The offers list has search and a status filter; opening or starting an offer replaces the list with the offer screen (customer, dates, lines with live totals, notes and the status buttons). Lines are added by searching products or as free lines.
- **PDF export** (`ExportSalesPdf`, QuestPDF Community license): the offer, order, pick list and invoice screens each have an Export PDF button that renders the document through the same Get handlers the screen uses, in the current UI language, and hands it to the user through `IFileSaveService` (`SalesPdfSaver`). One renderer (`SalesPdfRenderer`) draws a neutral `PdfDocumentModel`; the pick list has an empty "picked" column to write in. QuestPDF 2026.9+ ships only Lato, so `UseSystemFonts` is on for CJK text and `ThrowOnMissingTextGlyphs` is off: an unknown character is drawn as a replacement glyph rather than blocking the export. Error code `sales_pdf_kind_invalid`.

## Cvs

Each user keeps several CVs aimed at different jobs: `Cv` (`UserId`, unique case-insensitive `Title` per user, optional `TargetJob`, `Summary`). Handlers are scoped by `UserId`. Error codes `cv_*`.

- **UI**: its own sidebar page (signed-in user) and a CVs tab on the User detail page via `IUserCvsPanelFactory` (`Shared/Cvs`). Each CV opens a read-only detail page (owner details from `IUserLookup`, photo, summary, work experience, education, courses, languages, driver licence); entries are changed in an editor that replaces the overview.
- **Photo**: stored per CV (PNG/JPEG, max 6 MB), picked through `IFileOpenService` (`Shared/Files`, dialog in the Host).
- **Contact details** are the user's own data from Contact, read through `IContactInfoSource` (`Shared/Contacts`, one implementation per area, so Cvs never references them). Each CV stores only which entry ids to show (`CvContactSelection`: ids of other modules, so no foreign key; `SetCvContacts` ignores ids that are not the user's; deleted ones just disappear), plus an optional `LinkedIn` URL (normalized to https, must be on linkedin.com, error `cv_linkedin_invalid`).
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

Users, clients, contact persons, students, products and customers can have a photo (PNG/JPEG, max 6 MB), stored as `Photo` bytes in the owning module's database. Cvs keeps its own per-CV photo.

- `Get<X>Photo` / `Set<X>Photo` handlers read and write only that column, so lists and updates never load it.
- Shared pieces in `Shared/Photos`: `PhotoRules`, `PhotoThumbnail`, `BytesToBitmapConverter`, `PhotoPanelViewModel` / `PhotoPanelView` (round photo or initials with Change/Remove buttons on the User and Client detail pages) and `AvatarView`. Each module hands the panel its load/save delegates through its `<X>PhotoPanelFactory`; the file dialog is `IFileOpenService`.
- Error codes `photo_too_large` / `photo_invalid` (shared strings).
- Every saved photo also gets a 96x96 PNG `PhotoThumbnail` (SkiaSharp, made by the `Set<X>Photo` handler; bytes that cannot be decoded are `photo_invalid`). List queries and DTOs carry only the thumbnail (`Thumbnail`), shown by `AvatarView` in the Users, Clients and Contact persons lists, the sidebar and the top bar. Photos saved before thumbnails existed show initials in lists until they are uploaded again.
- The signed-in user's picture reaches the shell through `IUserPhotoLookup` (implemented by Users). `SetUserPhoto` publishes `UserPhotoChanged`, which the Host's `UserPhotoNotifier` turns into a refresh of the open shell.

## Audit

Append-only audit log of what users do, with a read-only viewer page ("Audit log", always on, `CanDisable: false`).

- **What is recorded**: every handler call, reads included, through the same `AppTelemetry.TraceAsync` wrapper that traces it. One entry has the time, the signed-in user (id and username, null before sign-in), the operation (`Module.Feature`), read or change, the entity id (the request's `Id`, or the `Id` of the result), the outcome and error code, and the duration. Backup and restore are recorded explicitly in `DatabaseBackup`.
- **Fields**: for a change, the names of the request properties that were filled in (ids excluded). This is the fields *submitted*, not a diff against the old values, and never the values themselves. Names only keeps customer, contact and mail data out of the log.
- **How**: `Shared/Audit` has `AuditTrail` (static entry point the Host connects at startup), `IAuditSink` and `AuditEntry`. The module's `AuditWriter` queues entries and writes them to `audit.db` on a background loop, so an operation never waits for the log and a write failure only goes to the application log. It starts once the database is migrated; earlier entries wait in the queue and are flushed when the container is disposed.
- The module's own handlers (`Audit.*`) are not audited. Nothing updates or deletes entries, and `audit.db` is deliberately not in `DatabaseFiles.All`: backups do not contain it and a restore never rolls it back.
- Not covered: settings that are not handlers (language, module switches).
