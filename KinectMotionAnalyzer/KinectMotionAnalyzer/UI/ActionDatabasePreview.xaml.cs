using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using KinectMotionAnalyzer.Model;

namespace KinectMotionAnalyzer.UI
{
    /// <summary>
    /// Interaction logic for ActionDatabasePreview.xaml
    /// </summary>
    public partial class ActionDatabasePreview : Window
    {
        public int selectedActionId = -1;

        public ActionDatabasePreview()
        {
            InitializeComponent();
        }

        private void dbActionTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // populate action from database
            // query all action ids for current type
            try
            {
                dbActionIdList.Items.Clear();
                using (MotionDBContext dbcontext = new MotionDBContext())
                {
                    //MessageBox.Show(dbcontext.Actions.Count().ToString());
                    foreach (KinectAction cur_action in dbcontext.Actions)
                    {
                        if (cur_action.ActionName == dbActionTypeList.SelectedValue.ToString())
                            dbActionIdList.Items.Add(cur_action.Id);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
           
        }

        private void dbActionPreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (dbActionIdList.SelectedValue != null)
                selectedActionId = int.Parse(dbActionIdList.SelectedValue.ToString());

            this.DialogResult = true;
        }
    }
}
