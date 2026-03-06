using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LibraryManager
{
    
    public enum BookStatus { Available, Borrowed, Archived }

    public class Book
    {
        private int _id;
        private string _title;
        private string _author;
        private BookStatus _status;

        public int Id => _id;
        public string Title => _title;
        public BookStatus Status { get => _status; set => _status = value; }

        public Book(int id, string title, string author)
        {
            _id = id;
            _title = title;
            _author = author;
            _status = BookStatus.Available;
        }
    }

    public class Reader
    {
        private int _ticketNumber;
        private string _fullName;
        private float _unpaidFines;

        public int TicketNumber => _ticketNumber;
        public string FullName => _fullName;

        public Reader(int ticket, string name)
        {
            _ticketNumber = ticket; 
            _fullName = name; 
            _unpaidFines = 0.0f;
        }

        public bool CanBorrow() => _unpaidFines <= 0;
    }

    public interface IRepository<T>
    {
        void Add(T item);     
        void Remove(int id);   
        T GetById(int id);      
        IEnumerable<T> GetAll();
    }

   
    public class BookRepository : IRepository<Book>
    {
        private List<Book> _books = new List<Book>();

        public void Add(Book book) => _books.Add(book);

        public void Remove(int id)
        {
            Book book = GetById(id);
            if (book != null) _books.Remove(book);
        }

        public Book GetById(int id) => _books.FirstOrDefault(b => b.Id == id);
        public IEnumerable<Book> GetAll() => _books;
    }

   
    public class ReaderRepository : IRepository<Reader>
    {
        private List<Reader> _readers = new List<Reader>();

        public void Add(Reader reader) => _readers.Add(reader);

        public void Remove(int ticket)
        {
            Reader reader = GetById(ticket);
            if (reader != null) _readers.Remove(reader);
        }

        public Reader GetById(int ticket) => _readers.FirstOrDefault(r => r.TicketNumber == ticket);
        public IEnumerable<Reader> GetAll() => _readers;
    }

    
    public partial class MainForm : Form
    {
        
        private BookRepository _bookRepo = new BookRepository();
        private ReaderRepository _readerRepo = new ReaderRepository();

        private TextBox txtBook = new TextBox { Location = new Point(150, 30), Width = 150 };
        private TextBox txtReader = new TextBox { Location = new Point(150, 70), Width = 150 };

        public MainForm()
        {
            InitializeData();
            InitializeInterface();
        }

        private void InitializeData()
        {
           
            _bookRepo.Add(new Book(1, "Гаррі Поттер", "Джоан Роулінг"));
            _bookRepo.Add(new Book(2, "Віднесені вітром", "Марґарет Мітчелл"));
            _readerRepo.Add(new Reader(88, "Олександр Донець"));
        }

        private void InitializeInterface()
        {
            this.Text = "Library Manager";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lblBook = new Label { Text = "ID Книги(1 чи 2):", Location = new Point(30, 30), AutoSize = true };
            Label lblReader = new Label { Text = "№ Квитка(88):", Location = new Point(30, 70), AutoSize = true };

            Button btnIssue = new Button
            {
                Text = "Оформити видачу",
                Location = new Point(130, 130),
                Size = new Size(140, 40),
                BackColor = Color.LightPink
            };

            btnIssue.Click += btnIssue_Click;

            this.Controls.AddRange(new Control[] { lblBook, txtBook, lblReader, txtReader, btnIssue });
        }

        private void btnIssue_Click(object sender, EventArgs e)
        {
           
            if (int.TryParse(txtBook.Text, out int bId) && int.TryParse(txtReader.Text, out int rId))
            {
               
                Book book = _bookRepo.GetById(bId);
                Reader reader = _readerRepo.GetById(rId);

                if (book != null && reader != null)
                {
                    if (book.Status == BookStatus.Available && reader.CanBorrow())
                    {
                        book.Status = BookStatus.Borrowed;
                        MessageBox.Show($"Успіх! Книга '{book.Title}' видана читачу {reader.FullName}.");
                    }
                    else
                    {
                        MessageBox.Show("Книга вже видана або у читача є борги.");
                    }
                }
                else
                {
                    MessageBox.Show("Об'єкт не знайдено.");
                }
            }
        }
    }
}