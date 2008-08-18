/*
 * Copyright (c) 2007-2008, openmetaverse.org
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without 
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the openmetaverse.org nor the names 
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OpenMetaverse.GUI
{

    /// <summary>
    /// TreeView GUI component for browsing a client's inventory
    /// </summary>
    public class InventoryTree : TreeView
    {
        private GridClient _Client;

        /// <summary>
        /// Gets or sets the GridClient associated with this control
        /// </summary>
        public GridClient Client
        {
            get { return _Client; }
            set { if (value != null) InitializeClient(value); }
        }

        /// <summary>
        /// TreeView control for an unspecified client's inventory
        /// </summary>
        public InventoryTree()
        {
            this.BeforeExpand += new TreeViewCancelEventHandler(InventoryTree_BeforeExpand);
        }

        /// <summary>
        /// TreeView control for the specified client's inventory
        /// </summary>
        /// <param name="client"></param>
        public InventoryTree(GridClient client) : this ()
        {
            InitializeClient(client);
        }

        /// <summary>
        /// Thread-safe method for clearing the TreeView control
        /// </summary>
        public void ClearNodes()
        {
            if (this.InvokeRequired) this.BeginInvoke((MethodInvoker)delegate { ClearNodes(); });
            else this.Nodes.Clear();
        }

        /// <summary>
        /// Thread-safe method for collapsing a TreeNode in the control
        /// </summary>
        /// <param name="node"></param>
        public void CollapseNode(TreeNode node)
        {
            if (this.InvokeRequired) this.BeginInvoke((MethodInvoker)delegate { CollapseNode(node); });
            else if (!node.IsExpanded) node.Collapse();
        }

        /// <summary>
        /// Thread-safe method for expanding a TreeNode in the control
        /// </summary>
        /// <param name="node"></param>
        public void ExpandNode(TreeNode node)
        {
            if (this.InvokeRequired) this.BeginInvoke((MethodInvoker)delegate { ExpandNode(node); });
            else if (!node.IsExpanded) node.Expand();
        }

        /// <summary>
        /// Thread-safe method for updating the contents of the specified folder UUID
        /// </summary>
        /// <param name="folderID"></param>
        public void UpdateFolder(InventoryFolder folder)
        {
            if (this.InvokeRequired) this.BeginInvoke((MethodInvoker)delegate { UpdateFolder(folder); });
            else
            {
                TreeNode node = null;
                TreeNodeCollection children;

                if (folder != Client.InventoryStore.RootFolder)
                {
                    TreeNode[] found = Nodes.Find(folder.UUID.ToString(), true);
                    if (found.Length > 0)
                    {
                        node = found[0];
                        children = node.Nodes;
                    }
                    else
                    {
                        Logger.Log("Received update for unknown TreeView node " + folder.UUID, Helpers.LogLevel.Warning);
                        return;
                    }
                }
                else children = this.Nodes;

                children.Clear();

                List<InventoryBase> contents = folder.Contents;
                if (contents.Count == 0)
                {
                    TreeNode add = children.Add(null, "(empty)");
                    add.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
                }
                else
                {
                    foreach (InventoryBase inv in contents)
                    {
                        string key = inv.UUID.ToString();
                        children.Add(key, inv.Name);
                        children[key].Tag = inv;
                        if (inv is InventoryFolder)
                        {
                            children[key].Nodes.Add(null, "(loading...)").ForeColor = Color.FromKnownColor(KnownColor.GrayText);
                            ((InventoryFolder)inv).OnContentsRetrieved += new InventoryFolder.ContentsRetrieved(InventoryFolder_OnContentsRetrieved);
                        }
                    }
                }
            }
        }

        private void InitializeClient(GridClient client)
        {
            _Client = client;
            _Client.Inventory.OnSkeletonsReceived += new InventoryManager.SkeletonsReceived(Inventory_OnSkeletonsReceived);
        }

        private void Inventory_OnSkeletonsReceived(InventoryManager manager)
        {
            _Client.InventoryStore.RootFolder.OnContentsRetrieved += new InventoryFolder.ContentsRetrieved(InventoryFolder_OnContentsRetrieved);
            UpdateFolder(_Client.InventoryStore.RootFolder);
        }

        private void InventoryFolder_OnContentsRetrieved(InventoryFolder folder)
        {
            UpdateFolder(folder);
        }

        private void InventoryTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            InventoryFolder folder = (InventoryFolder)e.Node.Tag;
            if (folder.IsStale) folder.RequestContents(InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);
            else UpdateFolder(folder);
        }

    }

}
