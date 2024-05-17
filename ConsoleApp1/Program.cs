using System;
using System.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;

namespace HarithmaPrinterService
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string rabbitUsername = Environment.GetEnvironmentVariable("RABBIT_MQ_USERNAME");
            string rabbitPassword = Environment.GetEnvironmentVariable("RABBIT_MQ_PASSWORD");
            string rabbitHost = Environment.GetEnvironmentVariable("RABBIT_MQ_HOST");

            var factory = new ConnectionFactory() { HostName = rabbitHost, UserName = rabbitUsername, Password = rabbitPassword };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "harithmaq", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = System.Text.Encoding.UTF8.GetString(body);
                    Console.WriteLine("Invoice Submitted {0}", message);

                    dynamic jsonObject = JsonConvert.DeserializeObject(message);

                    if (jsonObject.invoice_type == 1)
                    {
                        printServiceReceipt(jsonObject);
                        Console.WriteLine("Invoice printed");
                    }
                    else if (jsonObject.invoice_type == 2)
                    {
                        printItemReceipt(jsonObject);
                        Console.WriteLine("Invoice printed");
                    }
                };
                channel.BasicConsume(queue: "harithmaq", autoAck: true, consumer: consumer);

                Console.WriteLine(" [*] Waiting for messages. To exit press CTRL+C");
                Console.ReadLine(); // Keep the console application running until user closes it
            }
        }

        static void printItemReceipt(dynamic jsonObject)
        {
            Reports.ItemInvoiceReceipt itemInvoice = new Reports.ItemInvoiceReceipt();

            itemInvoice.SetDataSource(generateDataSet(jsonObject.invoice_details));
            itemInvoice.SetParameterValue("invoiceNumber", jsonObject.invoice_number);
            itemInvoice.SetParameterValue("invoiceTotalPrice", jsonObject.total_price);
            itemInvoice.SetParameterValue("invoiceDiscount", jsonObject.discount_pct);
            itemInvoice.SetParameterValue("invoicePaidAmount", jsonObject.paid_amount);
            itemInvoice.SetParameterValue("invoiceGrossPrice", jsonObject.gross_price);
            itemInvoice.SetParameterValue("invoicebalance", jsonObject.gross_price);

            itemInvoice.PrintOptions.PaperOrientation = CrystalDecisions.Shared.PaperOrientation.Portrait;
            itemInvoice.PrintToPrinter(1, false, 0, 0);
        }

        static void printServiceReceipt(dynamic jsonObject)
        {
            Reports.ServiceInvoiceReceipt seviceInvoice = new Reports.ServiceInvoiceReceipt();

            seviceInvoice.SetDataSource(generateDataSet(jsonObject.invoice_details));
            seviceInvoice.SetParameterValue("invoiceNumber", jsonObject.invoice_number);
            seviceInvoice.SetParameterValue("invoiceTotalPrice", jsonObject.total_price);
            seviceInvoice.SetParameterValue("invoiceDiscount", jsonObject.discount_pct);
            seviceInvoice.SetParameterValue("invoicePaidAmount", jsonObject.paid_amount);
            seviceInvoice.SetParameterValue("invoiceGrossPrice", jsonObject.gross_price);
            seviceInvoice.SetParameterValue("invoicebalance", jsonObject.gross_price);
            seviceInvoice.SetParameterValue("customerName", jsonObject.customer_name.ToString());
            seviceInvoice.SetParameterValue("employeeName", jsonObject.employee_name.ToString());
            seviceInvoice.SetParameterValue("vehicalNumber", jsonObject.vehical_number.ToString());
            seviceInvoice.SetParameterValue("washBay", jsonObject.wash_bay.ToString());
            seviceInvoice.SetParameterValue("currentMilage", jsonObject.current_milage);
            seviceInvoice.SetParameterValue("nextMilage", jsonObject.next_milage);

            seviceInvoice.PrintToPrinter(1, false, 0, 0);
        }

        static dynamic generateDataSet(dynamic invoiceDetails) 
        {
            DataSet InvoiceDetailsDS = new DataSet();
            DataTable InvoiceDetailsDT = new DataTable();

            InvoiceDetailsDT.Columns.Add("itemName", typeof(String));
            InvoiceDetailsDT.Columns.Add("itemUnitPrice", typeof(decimal));
            InvoiceDetailsDT.Columns.Add("itemQuantity", typeof(int));
            InvoiceDetailsDT.Columns.Add("itemTotalPrice", typeof(decimal));

            foreach (var item in invoiceDetails)
            {
                InvoiceDetailsDT.Rows.Add(item.item_name, item.unit_price, item.quantity, item.total_price);
            }
            InvoiceDetailsDS.Tables.Add(InvoiceDetailsDT);

            return InvoiceDetailsDS;
        }
    }
}
