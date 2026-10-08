# Розділ 2. Реєстр дефектів проєктування коду

## Зведений реєстр

Частота обмежується значенням 4. Пріоритет = серйозність × частота.

| ID | Місце у baseline | Дефект | Серйозність | Частота | Пріоритет |
|---|---|---|---:|---:|---:|
| D-01 | `LoanManager.cs:14–187` | Довгий метод | 5 | 1 | 5 |
| D-02 | `LoanManager.cs:189–208`; `LoanRepository.cs:56–81`; `LoanReport.cs:7–44` | Дробовик: правило пені у трьох класах | 5 | 3 | 15 |
| D-03 | `LoanManager.cs:14–22` | Довгий список параметрів | 4 | 1 | 4 |
| D-04 | `LegacyModels.cs:3–44`; `LoanManager.cs:81–114,210–230` | Одержимість елементарними типами | 4 | 4 | 16 |
| D-05 | `LoanManager.cs:26–79` | Надмірна вкладеність | 5 | 1 | 5 |
| D-06 | `LoanManager.cs:34,81–103,122–130` | Магічні числа | 4 | 4 | 16 |
| D-07 | `LoanManager.cs:12,116–141` | Тимчасове поле `_lastFine` | 3 | 1 | 3 |
| D-08 | `LoanManager.cs:157–164`; `LoanRepository.cs:83–94` | Проковтнутий виняток | 5 | 2 | 10 |
| D-09 | `LoanManager.cs:184–185` | Мертвий код | 2 | 1 | 2 |
| D-10 | `LoanReport.cs:46–61` | Заздрість до чужих даних | 3 | 1 | 3 |
| D-11 | `LegacyModels.cs:3–44` | Неінкапсульовані поля й колекція | 4 | 4 | 16 |
| D-12 | `LoanManager.cs:20–21,166–177` | Булеві параметри-прапорці | 3 | 2 | 6 |

Реєстр містить 12 записів і 12 типів дефектів. Профільний дефект варіанта
2 — D-02. Його обов'язкові рефакторинги: R10 Move Method і R09 Extract
Class.

## Картки дефектів

### D-01 — довгий метод

- **Місце:** `LoanManager.cs:14–187`, метод `Issue`.
- **Доказ:** 84 логічні рядки, CC = 26, вкладеність = 7.

```csharp
public LoanResult Issue(
    Reader? reader,
    List<BookCopy>? books,
    DateTime issuedOn,
    DateTime? returnedOn,
    string subscription,
    bool sendEmail,
    bool printReceipt,
    string operatorName)
```

- **Чим шкодить:** валідація, строк, пеня, звіт, збереження й сповіщення
  змінюються в одному методі; потрібно щонайменше 26 шляхів тестування.
- **Вплив:** читаність, змінюваність, тестованість.
- **Оцінка:** 5/5; частота 1; пріоритет 5.
- **Гіпотеза:** R01 Extract Method після побудови страхувальної сітки.

### D-02 — дробовик, профільний дефект

- **Місце:** `PreviewFine`, `SumOutstandingFines`, `BuildOverdueReport`.
- **Доказ:** одна формула повторена у трьох класах.

```csharp
int overdueDays = (onDate.Date - loan.DueOn.Date).Days;
fine = overdueDays * 2m * loan.Books.Count;
if (loan.Reader != null && loan.Reader.Category == "child")
{
    fine = fine * 0.5m;
}
if (fine > 500m)
{
    fine = 500m;
}
```

- **Чим шкодить:** зміна ставки або межі потребує синхронного редагування
  трьох файлів і легко породжує різні результати.
- **Вплив:** змінюваність, надійність, тестованість.
- **Оцінка:** 5/5; частота 3; пріоритет 15.
- **Гіпотеза:** R09 Extract Class `FineCalculator`, потім R10 Move Method.

### D-03 — довгий список параметрів

- **Місце:** `LoanManager.cs:14–22`, `Issue`.
- **Доказ:** вісім параметрів, два з яких є прапорцями.

```csharp
Reader? reader,
List<BookCopy>? books,
DateTime issuedOn,
DateTime? returnedOn,
string subscription,
bool sendEmail,
bool printReceipt,
string operatorName)
```

- **Чим шкодить:** легко переплутати дати та прапорці; кожна нова опція
  змінює сигнатуру й усі виклики.
- **Вплив:** читаність, змінюваність, тестованість.
- **Оцінка:** 4/5; частота 1; пріоритет 4.
- **Гіпотеза:** R11 Introduce Parameter Object `IssueRequest`.

### D-04 — одержимість елементарними типами

- **Місце:** рядкові `Category`, `Group`, `Status`, `subscription`.
- **Доказ:** допустимі значення існують лише як літерали.

```csharp
public string Category = "regular";
public string Group = "regular";
public string Status = "new";
if (subscription == "teacher")
{
    days = 30;
}
loan.Status = "active";
```

- **Чим шкодить:** друкарська помилка компілюється й утворює невалідний
  стан; пошук використань не гарантує повноти.
- **Вплив:** надійність, змінюваність.
- **Оцінка:** 4/5; частота 4; пріоритет 16.
- **Гіпотеза:** R12 Replace Primitive with Value Object/enum.

### D-05 — надмірна вкладеність

- **Місце:** `LoanManager.cs:26–79`, початок `Issue`.
- **Доказ:** сім рівнів за методикою роботи.

```csharp
if (reader != null)
{
    if (!reader.IsBlocked)
    {
        if (books != null)
        {
            if (books.Count > 0)
            {
                if (reader.ActiveLoans < 5 || reader.Category == "staff")
```

- **Чим шкодить:** головний сценарій зсунений праворуч, а причина кожного
  раннього завершення читається лише після проходження всього дерева.
- **Вплив:** читаність, тестованість.
- **Оцінка:** 5/5; частота 1; пріоритет 5.
- **Гіпотеза:** R06 Replace Nested Conditional with Guard Clauses.

### D-06 — магічні числа

- **Місце:** `LoanManager.cs:34,81–103,122–130`.
- **Доказ:** правила предметної області записані літералами.

```csharp
if (reader.ActiveLoans < 5 || reader.Category == "staff")
int days = 14;
days = 30;
days = 7;
days = 3;
_lastFine = overdueDays * 2m * books.Count;
if (_lastFine > 500m)
```

- **Чим шкодить:** значення не мають назв, а `2m` і `500m` продубльовані.
- **Вплив:** читаність, змінюваність.
- **Оцінка:** 4/5; частота 4; пріоритет 16.
- **Гіпотеза:** R04 Replace Magic Number with Constant.

### D-07 — тимчасове поле

- **Місце:** `_lastFine`, `LoanManager.cs:12,116–141`.
- **Доказ:** поле використовується лише всередині одного виклику `Issue`.

```csharp
private decimal _lastFine;
// ...
_lastFine = 0m;
if (returnedOn != null)
{
    // ...
    _lastFine = overdueDays * 2m * books.Count;
    loan.Fine = _lastFine;
}
```

- **Чим шкодить:** результат одного виклику стає станом об'єкта; паралельні
  виклики можуть впливати один на одного.
- **Вплив:** надійність, тестованість.
- **Оцінка:** 3/5; частота 1; пріоритет 3.
- **Гіпотеза:** R15 Replace Temp with Query або локальною змінною.

### D-08 — проковтнутий виняток

- **Місце:** `LoanManager.cs:157–164`, `LoanRepository.cs:83–94`.
- **Доказ:** два блоки `catch` не зберігають тип і контекст помилки.

```csharp
try
{
    _repository.Save(loan);
}
catch
{
    // помилка не передається викликачу
}
```

- **Чим шкодить:** `Issue` повертає успіх навіть тоді, коли видачу не
  збережено; діагностувати втрату даних неможливо.
- **Вплив:** надійність, тестованість.
- **Оцінка:** 5/5; частота 2; пріоритет 10.
- **Гіпотеза:** R20 Replace Error Code with Exception; зміну поведінки
  виконувати лише окремим `fix:` після рефакторингу.

### D-09 — мертвий код

- **Місце:** `LoanManager.cs:184–185`.
- **Доказ:** закоментована гілка не виконується і не перевіряється.

```csharp
result.Success = true;
result.Loan = loan;
result.Receipt = receipt;

// Старий варіант обмежував видачу двома книгами.
// if (books.Count > 2) return new LoanResult();
return result;
```

- **Чим шкодить:** створює сумнів, чи має правило діяти, але не дає
  компілятору або тестам перевірити його.
- **Вплив:** читаність, змінюваність.
- **Оцінка:** 2/5; частота 1; пріоритет 2.
- **Гіпотеза:** R17 Remove Dead Code.

### D-10 — заздрість до чужих даних

- **Місце:** `LoanReport.cs:46–61`, `BuildReaderCard`.
- **Доказ:** метод читає п'ять членів `Reader` і не має власного стану.

```csharp
string text = reader.Name.Trim().ToUpperInvariant();
text += " [" + reader.Category + "]";
text += " active=" + reader.ActiveLoans;
text += " fine=" + reader.UnpaidFine.ToString("0.00", CultureInfo.InvariantCulture);
for (int i = 0; i < loans.Count; i++)
{
    if (loans[i].Reader != null && loans[i].Reader!.Id == reader.Id)
```

- **Чим шкодить:** зміни представлення читача змушують редагувати клас
  звітів; поведінка не розташована поруч із даними.
- **Вплив:** змінюваність, читаність.
- **Оцінка:** 3/5; частота 1; пріоритет 3.
- **Гіпотеза:** R10 Move Method частини форматування до `Reader`.

### D-11 — неінкапсульовані поля й колекція

- **Місце:** `LegacyModels.cs:3–44`.
- **Доказ:** усі поля публічні, список можна замінити або змінити ззовні.

```csharp
public class Loan
{
    public int Id;
    public Reader? Reader;
    public List<BookCopy> Books = new();
    public DateTime IssuedOn;
    public DateTime DueOn;
    public string Status = "new";
    public decimal Fine;
}
```

- **Чим шкодить:** неможливо гарантувати інваріанти строку, стану й складу
  видачі; будь-який клієнт обходить правила.
- **Вплив:** надійність, змінюваність, тестованість.
- **Оцінка:** 4/5; частота 4; пріоритет 16.
- **Гіпотеза:** R13 Encapsulate Field/Collection.

### D-12 — булеві параметри-прапорці

- **Місце:** `LoanManager.cs:20–21,166–177`.
- **Доказ:** два прапорці вмикають незалежні сценарії всередині `Issue`.

```csharp
bool sendEmail,
bool printReceipt,
// ...
if (sendEmail)
{
    // окремий сценарій
}
if (printReceipt)
{
    Console.Write(receipt);
}
```

- **Чим шкодить:** один метод має чотири комбінації режимів; виклик із
  `true, false` не пояснює намір без іменованих аргументів.
- **Вплив:** читаність, тестованість, змінюваність.
- **Оцінка:** 3/5; частота 2; пріоритет 6.
- **Гіпотеза:** R19 Remove Flag Argument і окремі методи сповіщення/друку.

