using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;          
using System.Text.Json;     

namespace LibraryManager
{
    public enum BookStatus { Available, Borrowed, Archived }

    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public BookStatus Status { get; set; }

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
        public int TicketNumber { get; set; }
        public string FullName { get; set; }
        public Reader() { }
        public Reader(int ticket, string name) { TicketNumber = ticket; FullName = name; }
        public bool CanBorrow() => true;
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
        private object _lastActiveTable;

        private const string BooksFile = "books.json";
        private const string ReadersFile = "readers.json";

        private DataGridView dgvBooks = new DataGridView { Location = new Point(20, 30), Size = new Size(450, 100), SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true };
        private DataGridView dgvReaders = new DataGridView { Location = new Point(20, 160), Size = new Size(450, 100), SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true };
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

            if (!_bookRepo.GetAll().Any())
            {
                _bookRepo.Add(new Book(1, "Гаррі Поттер", "Джоан Роулінг"));
                _bookRepo.Add(new Book(2, "Віднесені вітром", "Марґарет Мітчелл"));
            }

            if (!_readerRepo.GetAll().Any())
            {
                _readerRepo.Add(new Reader(88, "Олександр Донець"));
            }

            SetupEvents();
            this.Controls.AddRange(new Control[] { dgvBooks, dgvReaders, lblInfo, txtName, txtExtra, btnAddBook, btnAddReader, btnEdit, btnDelete, btnReturn, btnIssue });
            RefreshData();
        }

        private void SaveData()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string booksJson = JsonSerializer.Serialize(_bookRepo.GetAll(), options);
                string readersJson = JsonSerializer.Serialize(_readerRepo.GetAll(), options);

                File.WriteAllText(BooksFile, booksJson);
                File.WriteAllText(ReadersFile, readersJson);
            }
            catch (Exception ex) { MessageBox.Show("Помилка збереження: " + ex.Message); }
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
            catch (Exception ex) { MessageBox.Show("Помилка завантаження: " + ex.Message); }
        }

        private void SetupEvents()
        {
            dgvBooks.Enter += (s, e) => _lastActiveTable = dgvBooks;
            dgvReaders.Enter += (s, e) => _lastActiveTable = dgvReaders;

            dgvBooks.CellClick += (s, e) => {
                if (dgvBooks.SelectedRows.Count > 0)
                {
                    txtName.Text = dgvBooks.SelectedRows[0].Cells["Title"].Value?.ToString();
                    txtExtra.Text = dgvBooks.SelectedRows[0].Cells["Author"].Value?.ToString();
                }
            };

            dgvReaders.CellClick += (s, e) => {
                if (dgvReaders.SelectedRows.Count > 0)
                {
                    txtName.Text = dgvReaders.SelectedRows[0].Cells["FullName"].Value?.ToString();
                    txtExtra.Text = dgvReaders.SelectedRows[0].Cells["TicketNumber"].Value?.ToString();
                }
            };

            btnEdit.Click += (s, e) => {
                if (_lastActiveTable == dgvBooks && dgvBooks.SelectedRows.Count > 0)
                {
                    int id = (int)dgvBooks.SelectedRows[0].Cells["Id"].Value;
                    Book b = _bookRepo.GetById(id);
                    if (b != null) { b.Title = txtName.Text; b.Author = txtExtra.Text; }
                }
                else if (_lastActiveTable == dgvReaders && dgvReaders.SelectedRows.Count > 0)
                {
                    int id = (int)dgvReaders.SelectedRows[0].Cells["TicketNumber"].Value;
                    Reader r = _readerRepo.GetById(id);
                    if (r != null) { r.FullName = txtName.Text; }
                }
                RefreshData();
            };

            btnDelete.Click += (s, e) => {
                if (_lastActiveTable == dgvBooks && dgvBooks.SelectedRows.Count > 0)
                    _bookRepo.Remove((int)dgvBooks.SelectedRows[0].Cells["Id"].Value);
                else if (_lastActiveTable == dgvReaders && dgvReaders.SelectedRows.Count > 0)
                    _readerRepo.Remove((int)dgvReaders.SelectedRows[0].Cells["TicketNumber"].Value);
                RefreshData();
            };

            btnAddBook.Click += (s, e) => {
                int newId = _bookRepo.GetAll().Any() ? _bookRepo.GetAll().Max(b => b.Id) + 1 : 1;
                _bookRepo.Add(new Book(newId, txtName.Text, txtExtra.Text));
                RefreshData();
            };

            btnAddReader.Click += (s, e) => {
                if (int.TryParse(txtExtra.Text, out int ticket))
                {
                    _readerRepo.Add(new Reader(ticket, txtName.Text));
                    RefreshData();
                }
                else MessageBox.Show("Введіть число в поле №Квитка!");
            };
            btnReturn.Click += (s, e) => {
                if (dgvBooks.SelectedRows.Count > 0)
                {
                  
                    int id = (int)dgvBooks.SelectedRows[0].Cells["Id"].Value;
                    Book book = _bookRepo.GetById(id);

                  
                    if (book.Status == BookStatus.Borrowed)
                    {
                        book.Status = BookStatus.Available;
                        MessageBox.Show($"Книгу '{book.Title}' успішно повернуто!");
                        RefreshData();
                    }
                    else
                    {
                        MessageBox.Show("Ця книга і так знаходиться в бібліотеці.");
                    }
                }
            };


            btnIssue.Click += (s, e) => {
                if (dgvBooks.SelectedRows.Count > 0 && dgvReaders.SelectedRows.Count > 0)
                {
                    Book book = _bookRepo.GetById((int)dgvBooks.SelectedRows[0].Cells["Id"].Value);
                    if (book.Status == BookStatus.Available)
                    {
                        book.Status = BookStatus.Borrowed;
                        MessageBox.Show("Успішно видано!");
                        RefreshData();
                    }
                    else MessageBox.Show("Ця книга вже видана або в архіві");
                }
            };
        }

        
        private void RefreshData()
        {
            dgvBooks.DataSource = null;
            dgvBooks.DataSource = _bookRepo.GetAll().ToList();
            dgvReaders.DataSource = null;
            dgvReaders.DataSource = _readerRepo.GetAll().ToList();

            SaveData();
        }
    }
}