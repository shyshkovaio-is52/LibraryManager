using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace LibraryManager
{

    public enum BookStatus { Available, Borrowed, Archived }

    public class Book
    {
        [DisplayName("ID книги")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Назва книги обов'язкова!")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Назва має бути від 2 до 100 символів.")]

        [DisplayName("Назва твору")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Вкажіть автора книги!")]
        [DisplayName("Автор")]
        public string Author { get; set; }

        [DisplayName("Статус")]
        public BookStatus Status { get; set; }

        public Book() { }
        public Book(int id, string title, string author)
        {
            Id = id;
            Title = title;
            Author = author;
            Status = BookStatus.Available;
        }
    }

    public class Reader
    {
        [Range(1, 99999, ErrorMessage = "Номер квитка має бути числом від 1 до 99999.")]
        [DisplayName("№ Квитка")]
        public int TicketNumber { get; set; }

        [Required(ErrorMessage = "ПІБ читача не може бути порожнім.")]
        [RegularExpression(@"^[a-zA-Zа-яА-ЯіІїЇєЄ\s\-]+$", ErrorMessage = "ПІБ має містити лише літери.")]
        [DisplayName("ПІБ")]
        public string FullName { get; set; }

        public int CurrentBorrowedBookId { get; set; } = -1;

        public Reader() { }
        public Reader(int ticket, string name) { TicketNumber = ticket; FullName = name; }
        public bool CanBorrow() => CurrentBorrowedBookId == -1;
    }

    public class LibraryService
    {
        public void Issue(Book book, Reader reader)
        {
            if (book.Status != BookStatus.Available) throw new Exception("Книга недоступна (вже видана або в архіві)!");
            if (!reader.CanBorrow()) throw new Exception("У читача вже є книга на руках!");

            book.Status = BookStatus.Borrowed;
            reader.CurrentBorrowedBookId = book.Id;
        }

        public void Return(Book book, List<Reader> readers)
        {
            if (book.Status != BookStatus.Borrowed) throw new Exception("Ця книга не перебуває у читача!");
            var reader = readers.FirstOrDefault(r => r.CurrentBorrowedBookId == book.Id);
            if (reader != null) reader.CurrentBorrowedBookId = -1;
            book.Status = BookStatus.Available;
        }
    }

    public class Repository<T>
    {
        private List<T> _items = new List<T>();
        public void Add(T item) => _items.Add(item);
        public void Remove(int id)
        {
            T item = GetById(id);
            if (item != null) _items.Remove(item);
        }
        public T GetById(int id)
        {
            return _items.FirstOrDefault(item =>
            {
                var prop = item.GetType().GetProperty("Id") ?? item.GetType().GetProperty("TicketNumber");
                return prop != null && (int)prop.GetValue(item) == id;
            });
        }
        public IEnumerable<T> GetAll() => _items;
    }

    public partial class MainForm : Form
    {
        private Repository<Book> _bookRepo = new Repository<Book>();
        private Repository<Reader> _readerRepo = new Repository<Reader>();
        private readonly LibraryService _libraryService = new LibraryService();
        private object _lastActiveTable;

        private const string BooksFile = "books.json";
        private const string ReadersFile = "readers.json";

        private DataGridView dgvBooks = new DataGridView { Location = new Point(20, 30), Size = new Size(450, 100), SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private DataGridView dgvReaders = new DataGridView { Location = new Point(20, 160), Size = new Size(450, 100), SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private Label lblInfo = new Label { Text = "Назва/ПІБ | Автор/№Квитка:", Location = new Point(20, 290), AutoSize = true };
        private TextBox txtName = new TextBox { Location = new Point(20, 310), Width = 150 };
        private TextBox txtExtra = new TextBox { Location = new Point(180, 310), Width = 150 };
        private Button btnAddBook = new Button { Text = "Додати Книгу", Location = new Point(20, 340), Width = 110 };
        private Button btnAddReader = new Button { Text = "Додати Читача", Location = new Point(140, 340), Width = 110 };
        private Button btnEdit = new Button { Text = "Редагувати", Location = new Point(260, 340), Width = 100 };
        private Button btnDelete = new Button { Text = "Видалити", Location = new Point(370, 340), Width = 100, BackColor = Color.LightCoral };
        private Button btnReturn = new Button { Text = "Повернути книгу", Location = new Point(20, 440), Size = new Size(450, 40), BackColor = Color.LightGreen };
        private Button btnIssue = new Button { Text = "Видати вибране", Location = new Point(20, 390), Size = new Size(450, 40), BackColor = Color.LightPink };

        public MainForm()
        {
            this.Size = new Size(510, 550);
            this.Text = "Library Manager";
            this.StartPosition = FormStartPosition.CenterScreen;

            LoadData();
            SetupEvents();
            this.Controls.AddRange(new Control[] { dgvBooks, dgvReaders, lblInfo, txtName, txtExtra, btnAddBook, btnAddReader, btnEdit, btnDelete, btnReturn, btnIssue });
            RefreshData();
        }

        private bool ValidateEntity(object obj)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(obj);
            if (!Validator.TryValidateObject(obj, context, results, true))
            {
                MessageBox.Show(results[0].ErrorMessage, "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }
        private void SaveData()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(BooksFile, JsonSerializer.Serialize(_bookRepo.GetAll(), options));
                File.WriteAllText(ReadersFile, JsonSerializer.Serialize(_readerRepo.GetAll(), options));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при збереженні файлів: " + ex.Message, "Помилка IO", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                if (File.Exists(BooksFile))
                {
                    string json = File.ReadAllText(BooksFile);
                    var books = JsonSerializer.Deserialize<List<Book>>(json);
                    if (books != null) foreach (var b in books) _bookRepo.Add(b);
                }

                if (File.Exists(ReadersFile))
                {
                    string json = File.ReadAllText(ReadersFile);
                    var readers = JsonSerializer.Deserialize<List<Reader>>(json);
                    if (readers != null) foreach (var r in readers) _readerRepo.Add(r);
                }
            }
            catch (JsonException)
            {
                MessageBox.Show("Файл бази даних має неправильний формат або пошкоджений!", "Помилка JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Критична помилка при завантаженні: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void SetupEvents()
        {
            dgvBooks.Enter += (s, e) => _lastActiveTable = dgvBooks;
            dgvReaders.Enter += (s, e) => _lastActiveTable = dgvReaders;


            dgvBooks.CellClick += (s, e) =>
            {
                if (dgvBooks.SelectedRows.Count > 0)
                {
                    txtName.Text = dgvBooks.SelectedRows[0].Cells["Назва"].Value?.ToString();
                    txtExtra.Text = dgvBooks.SelectedRows[0].Cells["Автор"].Value?.ToString();
                }
            };
            dgvReaders.CellClick += (s, e) =>
            {
                if (dgvReaders.SelectedRows.Count > 0)
                {
                    txtName.Text = dgvReaders.SelectedRows[0].Cells["ПІБ"].Value?.ToString();
                    txtExtra.Text = dgvReaders.SelectedRows[0].Cells["Квиток"].Value?.ToString();
                }
            };


            btnAddBook.Click += (s, e) =>
            {
                string title = txtName.Text.Trim();
                string author = txtExtra.Text.Trim();
                if (_bookRepo.GetAll().Any(b => b.Title.Equals(title, StringComparison.OrdinalIgnoreCase) && b.Author.Equals(author, StringComparison.OrdinalIgnoreCase)))
                {
                    if (MessageBox.Show("Така книга вже є. Додати ще примірник?", "Дублікат", MessageBoxButtons.YesNo) == DialogResult.No) return;
                }
                int newId = _bookRepo.GetAll().Any() ? _bookRepo.GetAll().Max(b => b.Id) + 1 : 1;
                Book book = new Book(newId, title, author);
                if (ValidateEntity(book))
                {
                    _bookRepo.Add(book);
                    RefreshData();
                }
            };

            btnAddReader.Click += (s, e) =>
            {
                if (int.TryParse(txtExtra.Text, out int ticket))
                {
                    if (_readerRepo.GetAll().Any(r => r.TicketNumber == ticket))
                    {
                        MessageBox.Show("Цей номер квитка вже зареєстрований за іншим читачем!", "Дублікат", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    Reader reader = new Reader(ticket, txtName.Text);
                    if (ValidateEntity(reader))
                    {
                        _readerRepo.Add(reader);
                        RefreshData();
                    }
                }
                else MessageBox.Show("Номер квитка має бути цілим числом", "Помилка вводу", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            btnIssue.Click += (s, e) =>
            {
                if (dgvBooks.SelectedRows.Count > 0 && dgvReaders.SelectedRows.Count > 0)
                {
                    Book book = _bookRepo.GetById((int)dgvBooks.SelectedRows[0].Cells["Id"].Value);
                    Reader reader = _readerRepo.GetById((int)dgvReaders.SelectedRows[0].Cells["Квиток"].Value);
                    if (book.Status != BookStatus.Available) MessageBox.Show("Ця книга зараз недоступна!");
                    else if (!reader.CanBorrow()) MessageBox.Show("У цього читача вже є книга на руках!");
                    else
                    {
                        book.Status = BookStatus.Borrowed;
                        reader.CurrentBorrowedBookId = book.Id;
                        MessageBox.Show($"Книгу успішно видано читачу: {reader.FullName}");
                        RefreshData();
                    }
                }
            };

            btnReturn.Click += (s, e) =>
            {
                if (dgvBooks.SelectedRows.Count > 0)
                {
                    Book book = _bookRepo.GetById((int)dgvBooks.SelectedRows[0].Cells["Id"].Value);
                    if (book.Status == BookStatus.Borrowed)
                    {
                        var reader = _readerRepo.GetAll().FirstOrDefault(r => r.CurrentBorrowedBookId == book.Id);
                        if (reader != null) reader.CurrentBorrowedBookId = -1;
                        book.Status = BookStatus.Available;
                        MessageBox.Show("Книгу повернуто до бібліотеки.");
                        RefreshData();
                    }
                    else MessageBox.Show("Ця книга не була видана.");
                }
            };

            btnEdit.Click += (s, e) =>
            {
                if (_lastActiveTable == dgvBooks && dgvBooks.SelectedRows.Count > 0)
                {
                    int id = (int)dgvBooks.SelectedRows[0].Cells["Id"].Value;
                    Book b = _bookRepo.GetById(id);
                    if (b != null) { b.Title = txtName.Text; b.Author = txtExtra.Text; }
                }
                else if (_lastActiveTable == dgvReaders && dgvReaders.SelectedRows.Count > 0)
                {
                    int id = (int)dgvReaders.SelectedRows[0].Cells["Квиток"].Value;
                    Reader r = _readerRepo.GetById(id);
                    if (r != null) { r.FullName = txtName.Text; }
                }
                RefreshData();
            };



            btnDelete.Click += (s, e) =>
            {
                if (_lastActiveTable == dgvBooks && dgvBooks.SelectedRows.Count > 0)
                {
                    int id = (int)dgvBooks.SelectedRows[0].Cells["Id"].Value;
                    if (_bookRepo.GetById(id).Status == BookStatus.Borrowed)
                    {
                        MessageBox.Show("Неможливо видалити книгу, яка зараз видана читачу!", "Заборонено", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                    _bookRepo.Remove(id);
                }
                else if (_lastActiveTable == dgvReaders && dgvReaders.SelectedRows.Count > 0)
                {
                    int ticket = (int)dgvReaders.SelectedRows[0].Cells["Квиток"].Value;
                    if (!_readerRepo.GetById(ticket).CanBorrow())
                    {
                        MessageBox.Show("Неможливо видалити читача, поки він не поверне книгу!", "Заборонено", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                    _readerRepo.Remove(ticket);
                }
                RefreshData();
            };
        }

        private void RefreshData()
        {

            dgvBooks.DataSource = _bookRepo.GetAll().Select(b => new
            {
                ID = b.Id,
                Назва = b.Title,
                Автор = b.Author,
                Статус = b.Status == BookStatus.Available ? "Вільна" : "Видана"
            }).ToList();


            dgvReaders.DataSource = _readerRepo.GetAll().Select(r => new
            {
                Квиток = r.TicketNumber,
                ПІБ = r.FullName,
                Стан = r.CanBorrow() ? "Вільний" : "Має книгу"
            }).ToList();

            SaveData();
        }
    }
}
