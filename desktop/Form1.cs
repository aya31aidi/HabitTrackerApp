using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Web.Data;
using HabitTrackerApp.Models;
namespace desktop
{
    public partial class Form1 : Form
    {
        private WebContext _context;

        public Form1()
        {
            InitializeComponent();
            textBoxPassword.PasswordChar = '*';
            dataGridView1.CellClick += dataGridView1_CellContentClick;
            var optionsBuilder = new DbContextOptionsBuilder<WebContext>();
            optionsBuilder.UseSqlServer("Server=DESKTOP-IQONVA5;Database=HabitTrackerDatabase;Trusted_Connection=True;TrustServerCertificate=True;User Id=sa;Password=pass");

            _context = new WebContext(optionsBuilder.Options);

            LoadUsers();
        }

        private void LoadUsers()
        {
            var users = _context.Users
        .Select(u => new
        {
            u.Id,
            u.Name,
            u.Email,
            Password = "******",  // cacher le mot de passe
            u.CreatedAt
        })
        .ToList();

            dataGridView1.DataSource = users;
        }
        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var selectedRow = dataGridView1.Rows[e.RowIndex];

                textBoxID.Text = selectedRow.Cells["idDataGridViewTextBoxColumn"].Value.ToString();
                textBoxName.Text = selectedRow.Cells["nameDataGridViewTextBoxColumn"].Value.ToString();
                textBoxEmail.Text = selectedRow.Cells["emailDataGridViewTextBoxColumn"].Value.ToString();
                textBoxPassword.Text = selectedRow.Cells["passwordDataGridViewTextBoxColumn"].Value.ToString();
               
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var user = new User
            {
                Name = textBoxName.Text,
                Email = textBoxEmail.Text,
                Password = textBoxPassword.Text,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            LoadUsers();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBoxID.Text, out int id))
            {
                var user = _context.Users.Find(id);
                if (user != null)
                {
                    user.Name = textBoxName.Text;
                    user.Email = textBoxEmail.Text;
                    user.Password = textBoxPassword.Text;
                   

                    _context.SaveChanges();
                    LoadUsers();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBoxID.Text, out int id))
            {
                var user = _context.Users.Find(id);
                if (user != null)
                {
                    _context.Users.Remove(user);
                    _context.SaveChanges();
                    LoadUsers();
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBoxID.Clear();
            textBoxName.Clear();
            textBoxEmail.Clear();
            textBoxPassword.Clear();
         
        }
    }
}
