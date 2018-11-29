using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;

namespace Mana301_DataFilter
{
    class Program
    {
        /*
        What entries should we get rid of?
            Missing, invalid, or dupe emails (cannot verify uniqueness)
            Suspiciously high $ spent per week on coffee (junk data. Max about $50?)
            More than 7 days/week on downtown campus (junk data)
            0 days spent on downtown campus (Not our demographic)
            People who want the service but don’t want to pay (Don’t add 0s to the average)
            People that don’t drink hot drinks (Not our demographic)

        Notes on what to keep
            Keep people that don’t want to subscribe so we can get a rough percentage expectation of the student body that will subscribe
            Keep people with garbage team numbers; the fact that they wanted to support many teams doesn’t discard their data

        Extra bits the algo can determine
            Percentage of population interested
            Average price people are willing to pay
            Average $ spent on coffee per week (Not really that useful but might be cool?)

        Note that we will have to manually curate these results just to be sure
            */

        const int COLS = 9;
        const bool VERBOSE = false;

        static void Main()
        {
            var source = "C:\\Users\\AFour\\Desktop\\datasheet.csv";
            var destination = "C:\\Users\\AFour\\Desktop\\datasheet_2.csv";
            var table = GetTable(source);
            var validated = ValidateTable(table);
            DisplayData(validated);
            WriteTable(validated, destination);
        }

        static List<string[]> GetTable(string path)
        {
            var table = new List<string[]>();
            using (TextReader file = File.OpenText(path))
            {
                var reader = new CsvReader(file);
                string[] row;
                
                while (reader.Read())
                {
                    row = new string[COLS];
                    for (var i = 0; reader.TryGetField(i, out string value); i++)
                    {
                        row[i] = value;
                    }
                    table.Add(row);
                }
            }
            return table;
        }

        static List<string[]> ValidateTable(List<string[]> table)
        {
            var validated = table.ToList();
            var emails = new List<string>();

            /*
             * Rules:
             * Valid, unique emails
             * Coffee purchased per week > 0 and <= $50
             * Days on campus > 0 and <= 7
             * Willingness to pay > 0
             *
             * Columns:
             * 0: timestamp
             * 1: team
             * 2: $ spent per week
             * 3: Days downtown
             * 4: Where coffee is bought
             * 5: Would you subscribe
             * 6: Why not
             * 7: How much would you pay
             * 8: email
             */

            if (VERBOSE)
                Console.WriteLine("Rows before: " + validated.Count);

            for (var i = 0; i < validated.Count; i++)
            {
                var row = validated[i];

                // Validate emails
                if (!row[8].Contains('@') || emails.Contains(row[8]) ||
                    row[8].ToLower().StartsWith("no@") || row[8].ToLower().StartsWith("none@") || row[8].ToLower().StartsWith("non@"))
                {
                    if (VERBOSE)
                        Console.WriteLine("Line " + i + " Invalid email: " + row[8]);
                    validated.RemoveAt(i--);
                    continue;
                }
                emails.Add(row[8]);

                //Validate coffee purchase amount
                if (!int.TryParse(row[2], out int spentOnCoffee) || spentOnCoffee > 50 || spentOnCoffee <= 0)
                {
                    if (VERBOSE)
                        Console.WriteLine("Line " + i + " Invalid coffee spent per week: $" + row[2]);
                    validated.RemoveAt(i--);
                    continue;
                }

                //Validate days on campus
                if (!int.TryParse(row[3], out int daysDowntown) || daysDowntown > 7 || daysDowntown <= 0)
                {
                    if (VERBOSE)
                        Console.WriteLine("Line " + i + " Invalid days on campus: " + row[3]);
                    validated.RemoveAt(i--);
                    continue;
                }

                //Validate willingness to pay
                if (!int.TryParse(row[7], out int wouldPay) || (row[5].Equals("Yes") && wouldPay <= 0))
                {
                    if (VERBOSE)
                        Console.WriteLine("Line " + i + " Invalid willingness to pay: $" + row[7]);
                    validated.RemoveAt(i--);
                    continue;
                }
            }

            if (VERBOSE)
                Console.WriteLine("Rows after: " + validated.Count);

            return validated;
        }

        static void DisplayData(List<string[]> table)
        {
            /* Data:
             * Average $ willing to be spent
             * Percentage interested
             * Average $ spent on drinks per week
             */

            int avgWillingCount = 0;
            int avgWillingSum = 0;
            int percInterestCount = table.Count;
            int percInterestSum = 0;
            int avgSpentCount = table.Count;
            int avgSpentSum = 0;

            foreach (var row in table)
            {
                if (row[5] == "Yes")
                {
                    avgWillingCount++;
                    avgWillingSum += int.Parse(row[7]);
                    percInterestSum++;
                }
                avgSpentSum += int.Parse(row[2]);
            }

            Console.WriteLine("Average $ people are willing to spend: $" + (double)avgWillingSum / (double)avgWillingCount);
            Console.WriteLine("Percent interested in service: " + percInterestSum / (100 * percInterestCount));
            Console.WriteLine("Average $ people already spend: $" + (double)avgSpentSum / (double)avgSpentCount);
        }

        static void WriteTable(List<string[]> table, string path)
        {
            using(var writer = new StreamWriter(path))
            {
                var csvWriter = new CsvWriter(writer);
                foreach (var row in table)
                {
                    csvWriter.WriteField(row);
                    csvWriter.NextRecord();
                }
                csvWriter.Flush();
            }
        }
    }
}
