﻿using ShoppingSystem.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ShoppingSystem.Forms
{
    public partial class CheckoutForm: Form
    {
        private List<CartItem> cartItems;
   
        public CheckoutForm(List<CartItem> cartItems)
        {
            InitializeComponent();
            this.cartItems = cartItems;
            InitializeCartView();
        }

        private void InitializeCartView()
        {
            dgvCart.DataSource = null;
            dgvCart.AutoGenerateColumns = false;
            dgvCart.Columns.Clear();

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "商品名稱", DataPropertyName = "ProductName" });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "單價", DataPropertyName = "Price" });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "數量", DataPropertyName = "Quantity" });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "小計", DataPropertyName = "Subtotal" });

            dgvCart.DataSource = cartItems.Select(c => new
            {
                ProductName = c.Product.Name,
                Price = c.Product.Price,
                Quantity = c.Quantity,
                Subtotal = c.Product.Price * c.Quantity
            }).ToList();

            UpdateTotalLabel();
        }
        private void UpdateTotalLabel()
        {
            int total = cartItems.Sum(item => item.Product.Price * item.Quantity);
            lblTotal.Text = $"總金額：${total}";
        }
  
        private void btnConfirm_Click(object sender, EventArgs e)
        {

            if (cartItems.Count == 0)
            {
                MessageBox.Show("購物車是空的！");
                return;
            }

            int totalPrice = cartItems.Sum(item => item.Product.Price * item.Quantity);
            string userName = "Guest";

            string cntStr = @"Data Source= (LocalDB)\MSSQLLocalDB;" +
                @"AttachDBFilename = C:\Users\tengy\source\repos\ShoppingSystem\ShoppingSystem\Database.mdf;
                Integrated Security=True;";
            using (SqlConnection conn = new SqlConnection(cntStr))
            {
                conn.Open();
                SqlTransaction tx = conn.BeginTransaction();

                try
                {
                    //寫入Orders
                    string sqlOrder = "INSERT INTO Orders([OrderDate ],TotalPrice,UserName) VALUES(GETDATE(),@totalPrice,@userName); SELECT SCOPE_IDENTITY();";
                    SqlCommand cmdOrder = new SqlCommand(sqlOrder, conn, tx);
             
                    cmdOrder.Parameters.AddWithValue("@totalPrice", totalPrice);
                    cmdOrder.Parameters.AddWithValue("@userName", userName);
                    int orderId = Convert.ToInt32(cmdOrder.ExecuteScalar());


                    //寫入OrderItmes
                    foreach (var item in cartItems)
                    {
                        string sqlItem = "INSERT INTO OrderItems(OrderId,ProductId,Quantity,UnitPrice) VALUES(@orderId, @productId, @quantity, @unitPrice);";
                        SqlCommand cmdItem = new SqlCommand(sqlItem, conn, tx);

                        cmdItem.Parameters.AddWithValue("@orderId", orderId);
                        cmdItem.Parameters.AddWithValue("@productId", item.Product.Id);
                        cmdItem.Parameters.AddWithValue("@quantity", item.Quantity);
                        cmdItem.Parameters.AddWithValue("@unitPrice", item.Product.Price);
                        cmdItem.ExecuteNonQuery();
                    }
                    tx.Commit();
                    MessageBox.Show("訂單儲存成功！");
                }
                catch (Exception ex)
                {
                    {
                        tx.Rollback();
                        MessageBox.Show("訂單儲存失敗：" + ex.Message);
                    }
                }
            }


            MessageBox.Show("訂單已成立！");
            cartItems.Clear(); // 清空購物車
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
