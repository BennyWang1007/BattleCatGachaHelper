using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BattleCatWinForm;
struct CatUnits
{
    public int Id;
    public uint Count;
    public string Name;
    public int Rarity;
};

namespace BattleCatWinForm
{

    public partial class MainForm : Form
    {
        private IntPtr BCRHandle = IntPtr.Zero;
        private string BannerName = "2025-08-13 ~ 2025-08-18: 共有5位合作限定角色登場！★點圖確認詳細吧!!";
        private uint Seed = 12345678;
        private uint SimCount = 100;

        private bool[] selectedCellsA = new bool[1000];
        private bool[] selectedCellsB = new bool[1000];
        private bool[] selectedCellsGuaranteeA = new bool[1000];
        private bool[] selectedCellsGuaranteeB = new bool[1000];

        private int[] UnitsId = new int[2000];

        // Create a c++ like unordermap to store the current selected units and their counts
        private Dictionary<int, uint> selectedUnitsCount = new Dictionary<int, uint>();

        private Dictionary<int, string> unitIdToName;
        private Dictionary<int, int> unitIdToRarity;
        private Dictionary<string, int> unitNameToId;

        public MainForm()
        {
            InitializeComponent();
            ReadCatData();

            seedTextBox.Text = Seed.ToString();
            rollCountUpDown.Value = SimCount;

            // Set banners' combo box items
            List<string> banners = BattleCatData.BannerNames;

            // Sort banners by start date (descending), format: "yyyy-MM-dd ~ yyyy-MM-dd: description"
            banners.Sort((a, b) =>
            {
                string[] partsA = a.Split('~');
                string[] partsB = b.Split('~');
                DateTime startA = DateTime.ParseExact(partsA[0].Trim(), "yyyy-MM-dd", null);
                DateTime startB = DateTime.ParseExact(partsB[0].Trim(), "yyyy-MM-dd", null);
                DateTime endA = DateTime.ParseExact(partsA[1].Split(':')[0].Trim(), "yyyy-MM-dd", null);
                DateTime endB = DateTime.ParseExact(partsB[1].Split(':')[0].Trim(), "yyyy-MM-dd", null);
                int result = startB.CompareTo(startA);
                if (result == 0) result = endB.CompareTo(endA);
                return result;
            });

            bannerComboBox.Items.AddRange(banners.ToArray());
            bannerComboBox.SelectedIndex = 0;
            FetchBannerSelection();

            ResetCells();

            RunSimulation();
        }

        ~MainForm()
        {
            if (BCRHandle != IntPtr.Zero)
            {
                BattleCatLib.DestroyBattleCatRoll(BCRHandle);
                BCRHandle = IntPtr.Zero;
            }
            selectedCellsA = null;
            selectedCellsB = null;
            selectedCellsGuaranteeA = null;
            selectedCellsGuaranteeB = null;
        }

        private void ResetCells()
        {
            // BCRHandle = BattleCatLib.CreateBattleCatRoll(BannerName, Seed);
            var newHandle = BattleCatLib.CreateBattleCatRoll(BannerName, Seed);
            if (newHandle != IntPtr.Zero)
            {
                BCRHandle = newHandle;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to create BattleCatRoll from " + BannerName);
            }
            for (int i = 0; i < selectedCellsA.Length; i++)
            {
                selectedCellsA[i] = false;
                selectedCellsB[i] = false;
                selectedCellsGuaranteeA[i] = false;
                selectedCellsGuaranteeB[i] = false;
            }
        }

        private void RollButton_Click(object sender, EventArgs e)
        {
            // Read the seed from the TextBox and banner from the ComboBox
            FetchSeed();
            FetchBannerSelection();
            BattleCatLib.SetSeed(BCRHandle, Seed);
            BattleCatLib.SetBanner(BCRHandle, BannerName);

            RunSimulation();
        }

        private void RunSimulation()
        {
            // Reset BCR's seed
            BattleCatLib.SetSeed(BCRHandle, Seed);

            // Clear result grids and reset height
            resultGridA.Rows.Clear();
            resultGridB.Rows.Clear();

            resultGridA.Height = (int)(SimCount + 1) * resultGridCellHeight;
            resultGridB.Height = (int)(SimCount + 1) * resultGridCellHeight;

            uint seedBackup = Seed;

            // // Filling first column
            // string[] col1 = new string[SimCount];
            // for (uint i = 0; i < SimCount; i++)
            // {
            //     uint idx = BattleCatLib.Roll(BCRHandle);
            //     col1[i] = BattleCatLib.GetUnitName(BCRHandle, idx);
            // }

            // // Filling third column
            // BattleCatLib.SetSeed(BCRHandle, BattleCatLib.AdvanceSeed(seedBackup));
            // string[] col3 = new string[SimCount];
            // for (uint i = 0; i < SimCount; i++)
            // {
            //     uint idx = BattleCatLib.Roll(BCRHandle);
            //     col3[i] = BattleCatLib.GetUnitName(BCRHandle, idx);
            // }

            // Filling first column
            for (uint i = 0; i < SimCount; i++)
            {
                uint idx = BattleCatLib.RollUncheck(BCRHandle);
                // col1[i] = BattleCatLib.GetUnitName(BCRHandle, idx);
                UnitsId[i * 2] = BattleCatLib.GetUnitId(BCRHandle, idx);
            }

            // Filling third column
            BattleCatLib.SetSeed(BCRHandle, BattleCatLib.AdvanceSeed(seedBackup));
            for (uint i = 0; i < SimCount; i++)
            {
                uint idx = BattleCatLib.RollUncheck(BCRHandle);
                // col3[i] = BattleCatLib.GetUnitName(BCRHandle, idx);
                UnitsId[i * 2 + 1] = BattleCatLib.GetUnitId(BCRHandle, idx);
            }

            // Filling second column with guaranteed roll
            BattleCatLib.SetSeed(BCRHandle, seedBackup);
            string[] col2 = new string[SimCount];
            for (uint i = 0; i < SimCount; i++)
            {
                uint idx = BattleCatLib.RollGuaranteed(BCRHandle);
                col2[i] = BattleCatLib.GetUnitName(BCRHandle, idx);
            }

            // Filling fourth column with guaranteed roll
            BattleCatLib.SetSeed(BCRHandle, BattleCatLib.AdvanceSeed(seedBackup));
            string[] col4 = new string[SimCount];
            for (uint i = 0; i < SimCount; i++)
            {
                uint idx = BattleCatLib.RollGuaranteed(BCRHandle);
                col4[i] = BattleCatLib.GetUnitName(BCRHandle, idx);
            }

            // Insert a blank row for staggering
            resultGridB.Rows.Add();
            resultGridB.Rows[0].Height = resultGridCellHeight / 2; // half cell height for staggering

            // Fill in the result grids
            for (int i = 0; i < SimCount; i++)
            {
                // resultGridA.Rows.Add($"{i + 1}A", col1[i], col2[i]);
                // resultGridB.Rows.Add(col3[i], col4[i], $"{i + 1}B");
                resultGridA.Rows.Add($"{i + 1}A", unitIdToName.ContainsKey(UnitsId[i * 2]) ? unitIdToName[UnitsId[i * 2]] : "Unknown", col2[i]);
                resultGridB.Rows.Add(unitIdToName.ContainsKey(UnitsId[i * 2 + 1]) ? unitIdToName[UnitsId[i * 2 + 1]] : "Unknown", col4[i], $"{i + 1}B");

                // resultGridA.Rows.Add($"{i + 1}A", UnitsId[i * 2], col2[i]);
                // resultGridB.Rows.Add(UnitsId[i * 2 + 1], col4[i], $"{i + 1}B");
                ColorCellsByRarity(resultGridA[1, i]);
                ColorCellsByRarity(resultGridB[0, i]);
            }

            Seed = seedBackup;
            // BattleCatLib.DestroyBattleCatRoll(handle);
        }

        private void FetchBannerSelection()
        {
            // Read the banner from the ComboBox
            if (bannerComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a banner.");
                return;
            }
            BannerName = bannerComboBox.SelectedItem.ToString();
            ResetCells(); // Reset the BCRHandle with the new banner

            // Rrint the selected banner name for debugging
            System.Diagnostics.Debug.WriteLine($"Selected Banner: {BannerName}");
        }

        private void FetchSeed()
        {
            // Read the seed from the TextBox
            if (!uint.TryParse(seedTextBox.Text, out Seed))
            {
                MessageBox.Show("Invalid seed value. Please enter a valid number.");
                return;
            }
            BattleCatLib.SetSeed(BCRHandle, Seed);

            // Rrint the seed for debugging
            System.Diagnostics.Debug.WriteLine($"Seed set to: {Seed}");
        }

        private void FetchSimCount()
        {
            SimCount = (uint)rollCountUpDown.Value;
        }

        // Do resimulation when the bannerComboBox selection changes
        private void BannerComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FetchBannerSelection();
            BattleCatLib.SetBanner(BCRHandle, BannerName);
            RunSimulation();
        }

        private void RollCountUpDown_ValueChanged(object sender, EventArgs e)
        {
            FetchSimCount();
            RunSimulation();
        }

        private void SeedTextBox_TextChanged(object sender, EventArgs e)
        {
            FetchSeed();
            RunSimulation();
        }

        private void ResultGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            return; // Disable single click selection for now since it mousedown does the same thing
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            bool isA = (sender == resultGridA);
            DataGridView grid = ((DataGridView)(sender));
            grid.ClearSelection();

            // Skip clicks on the position column
            if (isA && e.ColumnIndex == 0 || !isA && e.ColumnIndex == 2)
            {
                return;
            }

            string gridName = isA ? "A" : "B";
            System.Diagnostics.Debug.WriteLine($"Cell clicked in resultGrid{gridName} at {e.RowIndex}, {e.ColumnIndex}");

            var cell = grid[e.ColumnIndex, e.RowIndex];
            if (isA && e.ColumnIndex == 2 || !isA && e.ColumnIndex == 1)
            {
                ToggleResultGuarantee(cell, isA);
                ToggleResult11Pulls(e.RowIndex, isA);
            }
            else
            {
                ToggleResult(cell, isA);
            }
        }

        private void ResultGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            ResultGrid_CellClick(sender, e);
        }

        private void ToggleResult(DataGridViewCell cell, bool isA)
        {
            bool[] selectedCells = isA ? selectedCellsA : selectedCellsB;
            //string unitName = cell.Value.ToString();
            int unitId = unitNameToId.ContainsKey(cell.Value.ToString()) ? unitNameToId[cell.Value.ToString()] : -1;
            if (selectedCells[cell.RowIndex])
            {
                if (parentCellMap.ContainsKey(cell))
                {
                    var parentCell = parentCellMap[cell];
                    System.Diagnostics.Debug.WriteLine($"Unselecting cells belonging to ({cell.Value})");
                    foreach (var childCell in childCellsMap[parentCell])
                    {
                        if (childCell != null && childCell.RowIndex >= 0)// && childCell.RowIndex < selectedCells.Length)
                        {
                            UnselectCell(childCell);
                        }
                    }
                    UnselectCell(parentCell);
                    childCellsMap.Remove(parentCellMap[cell]); // Remove the group mapping
                    parentCellMap.Remove(cell);
                }
                else
                {
                    UnselectCell(cell); // Unselect the cell
                }
            }
            else
            {
                SelectCell(cell, isA);
            }
        }

        private void SelectCell(DataGridViewCell cell, bool isA)
        {
            bool[] selectedCells;
            if (isA && cell.ColumnIndex == 2 || !isA && cell.ColumnIndex == 1)
            {
                selectedCells = isA ? selectedCellsGuaranteeA : selectedCellsGuaranteeB;
            }
            else
            {
                selectedCells = isA ? selectedCellsA : selectedCellsB;
            }
            if (!selectedCells[cell.RowIndex])
            {
                selectedCells[cell.RowIndex] = true;
                int unitId = unitNameToId.ContainsKey(cell.Value.ToString()) ? unitNameToId[cell.Value.ToString()] : -1;
                cell.Style.BackColor = Color.Gray; // Change color to indicate selection
                if (selectedUnitsCount.ContainsKey(unitId))
                {
                    selectedUnitsCount[unitId]++;
                }
                else
                {
                    selectedUnitsCount[unitId] = 1; // Initialize count to 1
                }
            }
            System.Diagnostics.Debug.WriteLine($"Selected cell {cell.Value} at ({cell.RowIndex}, {cell.ColumnIndex})");
        }

        private void UnselectCell(DataGridViewCell cell)
        {
            bool isA = (cell.DataGridView == resultGridA);
    
            bool[] selectedCells;
            if (isA && cell.ColumnIndex == 2 || !isA && cell.ColumnIndex == 1)
            {
            selectedCells = isA ? selectedCellsGuaranteeA : selectedCellsGuaranteeB;
            cell.Style.BackColor = Color.White;
            }
            else
            {
            selectedCells = isA ? selectedCellsA : selectedCellsB;
            ColorCellsByRarity(cell);
            }
            if (selectedCells[cell.RowIndex])
            {
            selectedCells[cell.RowIndex] = false;
            int unitId = unitNameToId.ContainsKey(cell.Value.ToString()) ? unitNameToId[cell.Value.ToString()] : -1;
            if (selectedUnitsCount.ContainsKey(unitId))
            {
                selectedUnitsCount[unitId]--;
                if (selectedUnitsCount[unitId] <= 0)
                {
                selectedUnitsCount.Remove(unitId);
                }
            }
            else
            {
                throw new InvalidOperationException($"Unit {unitId} not found in selectedUnitsCount, cannot decrement count.");
            }
            }
            if (cell.Tag == "forbidden")
            {
            cell.Tag = null; // Remove the tag if it was crossed
            UnCrossCell(cell); // Uncross the cell
            }
            System.Diagnostics.Debug.WriteLine($"Unselected cell {cell.Value} at ({cell.RowIndex}, {cell.ColumnIndex})");
        }

        private void ToggleResultGuarantee(DataGridViewCell cell, bool isA)
        {
            bool[] selectedCellsGuarantee = isA ? selectedCellsGuaranteeA : selectedCellsGuaranteeB;
            int unitId = unitNameToId.ContainsKey(cell.Value.ToString()) ? unitNameToId[cell.Value.ToString()] : -1;

            if (selectedCellsGuarantee[cell.RowIndex])
            {
                UnselectCell(cell);
                cell.Style.BackColor = Color.White; // Guarantee no need to color
            }
            else
            {
                SelectCell(cell, isA);
            }
        }

        private void ToggleResult11Pulls(int row, bool isA)
        {
            if (isA)
            {
                if (!selectedCellsGuaranteeA[row])
                {
                    var parentCell = resultGridA[2, row];
                    for (int i = 0; i < 10; ++i)
                    {
                        AddCellGroup(resultGridA[1, row + i], parentCell);
                    }
                    SelectGroup(resultGridA[1, row], isA);
                    AddCellGroup(resultGridB[0, row + 10], parentCell);
                    CrossCell(resultGridB[0, row + 10]);
                }
                else
                {
                    UnselectGroup(resultGridA[1, row], isA);
                    UnCrossCell(resultGridB[0, row + 10]);
                }
            }
            else
            {
                if (!selectedCellsGuaranteeB[row])
                {
                    var parentCell = resultGridB[1, row];
                    for (int i = 0; i < 10; ++i)
                    {
                        AddCellGroup(resultGridB[0, row + i], parentCell);
                    }
                    SelectGroup(resultGridB[0, row], isA);
                    AddCellGroup(resultGridA[1, row + 9], parentCell);
                    CrossCell(resultGridA[1, row + 9]);
                }
                else
                {
                    UnselectGroup(resultGridB[0, row], isA);
                    UnCrossCell(resultGridA[1, row + 9]);
                }
            }
        }

        private void SelectGroup(DataGridViewCell cell, bool isA)
        {
            var parentCell = parentCellMap.ContainsKey(cell) ? parentCellMap[cell] : null;
            if (parentCell == null)
            {
                SelectCell(cell, isA);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Selected group for cell {cell.Value} at ({cell.RowIndex}, {cell.ColumnIndex}), parent cell {parentCell.Value} at ({parentCell.RowIndex}, {parentCell.ColumnIndex})");

            var childCells = childCellsMap.ContainsKey(parentCell) ? childCellsMap[parentCell] : null;
            if (childCells != null)
            {
                foreach (var childCell in childCells)
                {
                    System.Diagnostics.Debug.WriteLine($"Selected child cell {childCell.Value} at ({childCell.RowIndex}, {childCell.ColumnIndex})");
                    SelectCell(childCell, isA);
                }
                SelectCell(parentCell, isA); // Select the parent cell first
            }
        }

        private void UnselectGroup(DataGridViewCell cell, bool isA)
        {
            var parentCell = parentCellMap.ContainsKey(cell) ? parentCellMap[cell] : null;
            if (parentCell == null)
            {
                UnselectCell(cell);
                return;
            }

            var childCells = childCellsMap.ContainsKey(parentCell) ? childCellsMap[parentCell] : null;
            if (childCells != null)
            {
                foreach (var childCell in childCells)
                {
                    UnselectCell(childCell);
                    parentCellMap.Remove(childCell); // Clear maps
                }
            }
            UnselectCell(parentCell);
            childCellsMap.Remove(parentCell); // Clear maps
        }

        private bool isSwiping = false;
        private DataGridViewCell swipeStartCell = null;
        private DataGridView swipeGrid = null;
        private int swipeLastRow = -1;

        private void ResultGrid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            bool isA = (sender == resultGridA);
            if (!isA && e.RowIndex == 0) return;
            if (isA)
            {
                resultGridA.ClearSelection();
                if (e.ColumnIndex == 2)
                {
                    // ToggleResultGuarantee(resultGridA[e.ColumnIndex, e.RowIndex], isA);
                    ToggleResult11Pulls(e.RowIndex, isA);
                    UpdateSelectedUnits();
                }
                if (e.ColumnIndex != 1) return;
            }
            else
            {
                resultGridB.ClearSelection();
                if (e.ColumnIndex == 1)
                {
                    // ToggleResultGuarantee(resultGridB[e.ColumnIndex, e.RowIndex], isA);
                    ToggleResult11Pulls(e.RowIndex, isA);
                    UpdateSelectedUnits();
                }
                if (e.ColumnIndex != 0) return;
            }
            isSwiping = true;
            swipeGrid = sender as DataGridView;
            swipeStartCell = swipeGrid[e.ColumnIndex, e.RowIndex];
            swipeLastRow = e.RowIndex;

            ToggleResult(swipeStartCell, isA);
            UpdateSelectedUnits();

            // Print debug info
            string gridName = isA ? "A" : "B";
            //System.Diagnostics.Debug.WriteLine($"Cell mouse down in resultGrid{gridName} at {e.RowIndex}, {e.ColumnIndex}");
        }

        private void ResultGrid_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (!isSwiping || swipeStartCell == null || swipeGrid != sender) return;
            if (e.ColumnIndex != swipeStartCell.ColumnIndex) return; // do not cross columns

            if (e.RowIndex == swipeLastRow) return; // no need to update if the row hasn't changed

            swipeGrid.ClearSelection();
            swipeLastRow = e.RowIndex;
            var cell = swipeGrid[e.ColumnIndex, e.RowIndex];
            bool isA = (swipeGrid == resultGridA);
            ToggleResult(cell, isA);
            UpdateSelectedUnits();

            // Print debug info
            string gridName = isA ? "A" : "B";
            //System.Diagnostics.Debug.WriteLine($"Cell mouse move in resultGrid{gridName} at {e.RowIndex}, {e.ColumnIndex}");
        }

        private void ResultGrid_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            resultGridA.ClearSelection();
            resultGridB.ClearSelection();

            isSwiping = false;
            swipeStartCell = null;
            swipeGrid = null;

            // Print debug info
            string gridName = (sender == resultGridA) ? "A" : "B";
            //System.Diagnostics.Debug.WriteLine($"Cell mouse up in resultGrid{gridName} at {e.RowIndex}, {e.ColumnIndex}");
        }


        private void ResetSelection(object sender, EventArgs e)
        {
            Array.Clear(selectedCellsA, 0, selectedCellsA.Length);
            Array.Clear(selectedCellsB, 0, selectedCellsB.Length);
            Array.Clear(selectedCellsGuaranteeA, 0, selectedCellsGuaranteeA.Length);
            Array.Clear(selectedCellsGuaranteeB, 0, selectedCellsGuaranteeB.Length);
            resultGridA.ClearSelection();
            resultGridB.ClearSelection();

            foreach (DataGridViewRow row in resultGridA.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    cell.Style.BackColor = Color.White; // Reset to default color
                }
            }
            foreach (DataGridViewRow row in resultGridB.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    cell.Style.BackColor = Color.White; // Reset to default color
                }
            }
        }

        private void UpdateSelectedUnits()
        {
            selectedUnitsGrid.Rows.Clear();
            // Sort selectedUnits by rarity than id
            // copy a list of selectedUnitsCount to sort

            var sortedUnits = new List<CatUnits>();
            foreach (var kvp in selectedUnitsCount)
            {
                sortedUnits.Add(new CatUnits
                {
                    Id = kvp.Key,
                    Count = kvp.Value,
                    Name = unitIdToName.ContainsKey(kvp.Key) ? unitIdToName[kvp.Key] : "Unknown",
                    Rarity = unitIdToRarity.ContainsKey(kvp.Key) ? unitIdToRarity[kvp.Key] : -1
                });
            }
            sortedUnits.Sort((a, b) =>
            {
                if (a.Rarity != b.Rarity) return b.Rarity.CompareTo(a.Rarity);
                if (a.Count != b.Count) return b.Count.CompareTo(a.Count);
                return b.Id.CompareTo(a.Id);
            });
            foreach (var unit in sortedUnits)
            {
                selectedUnitsGrid.Rows.Add(unit.Name, unit.Count);
            }

            // Update the height of the selected units grid
            selectedUnitsGrid.Height = (Math.Max(sortedUnits.Count, 10) + 2) * 24;
            System.Diagnostics.Debug.WriteLine($"Updated selectedUnitsGrid height: {selectedUnitsGrid.Height}");

            // Update the color by rarity
            foreach (DataGridViewRow row in selectedUnitsGrid.Rows)
            {
                DataGridViewCell nameCell = row.Cells[0];
                ColorCellsByRarity(nameCell);
                row.Cells[1].Style.BackColor = nameCell.Style.BackColor;
            }
            selectedUnitsGrid.ClearSelection();
        }

        private void ReadCatData()
        {
            // Read cat data from a yaml file
            var data = BattleCatDataLoader.LoadYaml("bc-tw.yaml");
            unitIdToName = BattleCatDataLoader.GetUnitIdToName(data);
            unitIdToRarity = BattleCatDataLoader.GetUnitIdToRarity(data);
            unitNameToId = BattleCatDataLoader.GetUnitNameToId(data);
        }

        // Create a method color the cells determined by it's rarity
        private void ColorCellsByRarity(DataGridViewCell cell)
        {
            //System.Diagnostics.Debug.WriteLine($"Coloring cell: {cell.Value}");
            //int id = unitNameToId.ContainsKey(cell.Value.ToString()) ? unitNameToId[cell.Value.ToString()] : -1;
            if (cell == null || cell.Value == null || !unitNameToId.ContainsKey(cell.Value.ToString()))
            {
                return;
            }

            int id = unitNameToId[cell.Value.ToString()];
            if (!unitIdToRarity.ContainsKey(id))
            {
                return;
            }

            int rarity = unitIdToRarity[id];
            if (rarity == 0)
            {
                cell.Style.BackColor = Color.White; // Common
            }
            else if (rarity == 1)
            {
                cell.Style.BackColor = Color.White; // Ex
            }
            else if (rarity == 2)
            {
                cell.Style.BackColor = Color.White; // Rare
            }
            else if (rarity == 3)
            {
                cell.Style.BackColor = Color.FromArgb(220, 220, 0); // Super Rare, dark yellow
            }
            else if (rarity == 4)
            {
                cell.Style.BackColor = Color.Red; // Uber 
            }
            else if (rarity == 5)
            {
                cell.Style.BackColor = Color.Purple; // Legend
            }
            else if (rarity == 6)
            {
                cell.Style.BackColor = Color.LightBlue; // Exclusive
            }
            else
            {
                cell.Style.BackColor = Color.Blue; // Unknown or other rarity
                System.Diagnostics.Debug.WriteLine($"Unknown rarity for unit {cell.Value}: {rarity}");
            }
        }

        private void ResultGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e, DataGridView grid)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

            // Example: Mark cell if it's in some "forbidden" list
            bool forbidden = cell.Tag as string == "forbidden";
            if (cell.Tag != null && cell.Tag is string tagValue)
            {
                System.Diagnostics.Debug.WriteLine($"Cell {cell.Value} has tag: {tagValue}");
            }
            if (forbidden)
            {
                System.Diagnostics.Debug.WriteLine($"Cell {cell.Value} is forbidden");
                // Let the default painting happen
                // e.Handled = false;
                e.PaintBackground(e.CellBounds, true);
                e.PaintContent(e.CellBounds);

                // Draw cross on top
                using (Pen pen = new Pen(Color.Gray, 2))
                {
                    // Diagonal line from top-left to bottom-right
                    e.Graphics.DrawLine(pen, e.CellBounds.Left + 2, e.CellBounds.Top + 2,
                                            e.CellBounds.Right - 2, e.CellBounds.Bottom - 2);

                    // Diagonal line from bottom-left to top-right
                    e.Graphics.DrawLine(pen, e.CellBounds.Left + 2, e.CellBounds.Bottom - 2,
                                            e.CellBounds.Right - 2, e.CellBounds.Top + 2);
                }

                e.Handled = true; // Tell DataGridView we did all the painting
            }
        }

        private void ResultGridA_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            ResultGrid_CellPainting(sender, e, resultGridA);
        }

        private void ResultGridB_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            ResultGrid_CellPainting(sender, e, resultGridB);
        }

        // A mapping of cells to their belonging cells (e.g., for grouping)
        private Dictionary<DataGridViewCell, DataGridViewCell> parentCellMap = new Dictionary<DataGridViewCell, DataGridViewCell>();
        // A mapping of cells to their child cells
        private Dictionary<DataGridViewCell, List<DataGridViewCell>> childCellsMap = new Dictionary<DataGridViewCell, List<DataGridViewCell>>();

        private void AddCellGroup(DataGridViewCell childCell, DataGridViewCell parentCell)
        {
            if (childCell == null || parentCell == null) return;

            System.Diagnostics.Debug.WriteLine($"Adding cell group: {childCell.Value} to {parentCell.Value}");

            // Add the child cell to the parent's group
            if (!childCellsMap.ContainsKey(parentCell))
            {
                childCellsMap[parentCell] = new List<DataGridViewCell>();
            }
            childCellsMap[parentCell].Add(childCell);
            parentCellMap[childCell] = parentCell;
        }

        private void CrossCell(DataGridViewCell cell)
        {
            if (cell == null) return;
            System.Diagnostics.Debug.WriteLine($"Crossing cell: {cell.Value} at {cell.RowIndex}, {cell.ColumnIndex}");
            cell.Tag = "forbidden";
            cell.DataGridView.InvalidateCell(cell);
        }

        private void UnCrossCell(DataGridViewCell cell)
        {
            if (cell == null) return;
            cell.Tag = null;
            cell.DataGridView.InvalidateCell(cell);
        }
    }
}
