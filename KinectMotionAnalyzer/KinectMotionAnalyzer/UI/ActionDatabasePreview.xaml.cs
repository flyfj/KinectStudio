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
        public Dictionary<string, int> actionDict = new Dictionary<string, int>();

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
                actionDict.Clear();
                using (MotionDBContext dbcontext = new MotionDBContext())
                {
                    //MessageBox.Show(dbcontext.Actions.Count().ToString());
                    foreach (KinectAction cur_action in dbcontext.Actions)
                    {
                        if (cur_action.ActionName == dbActionTypeList.SelectedValue.ToString())
                        {
                            actionDict.Add(cur_action.CurActionName, cur_action.Id);
                            dbActionIdList.Items.Add(cur_action.CurActionName);
                        }
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
                selectedActionId = actionDict[dbActionIdList.SelectedValue.ToString()];

            this.DialogResult = true;
        }

        private void dbActionPreviewDelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (dbActionIdList.SelectedValue != null)
            {
                //MessageBox.Show(dbActionIdList.SelectedValue.ToString());

                //// remove select action from database
                //using (MotionDBContext dbcontext = new MotionDBContext())
                //{
                //    //MessageBox.Show(dbcontext.Actions.Count().ToString());
                //    foreach (KinectAction cur_action in dbcontext.Actions)
                //    {
                //        if (cur_action.CurActionName == dbActionIdList.SelectedValue.ToString())
                //        {
                //            dbcontext.Actions.Remove(cur_action);
                //            break;
                //        }
                //    }

                //    dbcontext.SaveChanges();
                //}
            }
        }
    }
}
