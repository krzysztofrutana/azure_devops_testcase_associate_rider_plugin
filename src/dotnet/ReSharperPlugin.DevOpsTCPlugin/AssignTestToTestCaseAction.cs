using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.Components;
using JetBrains.DataFlow;
using JetBrains.IDE.UI;
using JetBrains.IDE.UI.Extensions;
using JetBrains.IDE.UI.Extensions.Properties;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Features.Internal.Resources;
using JetBrains.ReSharper.UnitTestFramework.Actions;
using JetBrains.ReSharper.UnitTestFramework.Criteria;
using JetBrains.ReSharper.UnitTestFramework.UI.ViewModels.TreeModel;
using JetBrains.Rider.Model.UIAutomation;
using ReSharperPlugin.DevOpsTCPlugin.Icons;
using ReSharperPlugin.DevOpsTCPlugin.Models;
using ReSharperPlugin.DevOpsTCPlugin.Settings;
using MessageBox = JetBrains.Util.MessageBox;

namespace ReSharperPlugin.DevOpsTCPlugin;

[Action(
    ResourceType: typeof(Resources),
    TextResourceName: nameof(Resources.ManageTokensTitle),
    DescriptionResourceName = nameof(Resources.Description),
    Icon = typeof(FeaturesInternalThemedIcons.QuickStartToolWindow))]
public class AssignTestToTestCaseAction : IExecutableAction
{
    public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
    {
        return true;
    }

    public void Execute(IDataContext context, DelegateExecute nextExecute)
    {
        var dialogHost = context.GetComponent<IDialogHost>();

        var iconHost = context.GetComponent<IconsProvider>();
        
        var projectDll = string.Empty;
        
        var tree = context.GetData(UnitTestDataConstants.TREE);

        var selectedNode = tree?.SelectedNode;
        
        if (selectedNode is null)
        {
            MessageBox.ShowError(Resources.IncorrectTest);
            return;
        }

        if (selectedNode.Value.Criterion is ProjectCriterion)
        {
            MessageBox.ShowError(Resources.ShouldSelectCurrentTest);
            return;
        }

        var selectedNodeValue = GetLastChild(selectedNode.Value);

        if (selectedNodeValue is null)
        {
            MessageBox.ShowError(Resources.IncorrectTest);
            return;
        }
        
        var criterion = selectedNodeValue.Criterion as TestAncestorCriterion;
        var testPath = criterion?.AncestorIds.FirstOrDefault()?.TestId;

        var solution = context.GetComponent<ISolution>();
        var projectId = criterion?.AncestorIds.FirstOrDefault()?.ProjectId;
        if (projectId != null)
        {
            var project = solution.GetProjectByGuid(Guid.Parse(projectId));
            var targetFramework = project?.TargetFrameworkIds
                .FirstOrDefault(x => x.UniqueString == criterion.AncestorIds
                    .FirstOrDefault()?.TargetFrameworkId);

            if (targetFramework != null)
            {
                projectDll = Path.GetFileName(project.GetOutputFilePath(targetFramework).FullPath);
            }
        }
        
        BeTextBox textBoxTestPath;
        BeTextBox textBoxDll;
        BeTextBox textBoxTcId;
        BeTextBox textBoxTestType;

        var settingsStore = context.GetComponent<DevOpsSettingsStore>();
        var settings = settingsStore.GetSettings();
        var clipboard = context.GetComponent<Clipboard>();
        
        dialogHost.Show(
            getDialog: lt =>
            {
                var grid = BeControls.GetAutoGrid();
                var alreadyAssignedWorkItems = new List<WorkItemInfo>();
                textBoxTestPath = BeControls.GetTextBox(lt, initialText: testPath);
                textBoxDll = BeControls.GetTextBox(lt, initialText: projectDll);

                #region TokenComboBox

                var manageTokenGrid = BeControls.GetAutoGrid(GridOrientation.Horizontal);

                var selectedToken = settings.TokensListEvent.FirstOrDefault(x => x.SolutionName == solution.Name);
                var comboBoxProperty = BeUtil.GetPropertyWithHandler(lt, "id", token =>
                {
                    selectedToken = token;
                    alreadyAssignedWorkItems = AzureDevOpsOperations.GetAlreadyAssignedItems(selectedToken, textBoxTestPath.Text.Value, textBoxDll.Text.Value);
                }, selectedToken);

                var combobox = comboBoxProperty.GetBeComboBox(lt, settings.TokensListEvent,
                    (_, element, _) =>
                        BeControls.BeLabel(element.ToString()));
                
                manageTokenGrid.AddElement(BeControls.BeLabel(Resources.Token));
                manageTokenGrid.AddElement(combobox, BeSizingType.Fill);
                manageTokenGrid.AddElement(ManageTokensDialogs.GetManageTokensDialog(solution, lt, settingsStore, settings, iconHost, dialogHost));

                #endregion
                
                grid.AddElement(manageTokenGrid);
                
                textBoxTcId = BeControls.GetTextBox(lt);
                textBoxTestType = BeControls.GetTextBox(lt);
                
                grid.AddElement(BeControls.GetAutoGrid()
                    .AddElements(BeControls.BeLabel(Resources.PathToTest), textBoxTestPath));
                
                grid.AddElement(
                    BeControls.GetAutoGrid(orientation: GridOrientation.Horizontal)
                        .AddElements(
                            BeControls.GetAutoGrid()
                                .AddElements(BeControls.BeLabel(Resources.ProjectDll), textBoxDll),
                            BeControls.GetAutoGrid()
                                .AddElements(BeControls.BeLabel(Resources.TestType), textBoxTestType)));

                #region AlreadyAssignedTreeGrid

                alreadyAssignedWorkItems = AzureDevOpsOperations.GetAlreadyAssignedItems(selectedToken, textBoxTestPath.Text.Value, textBoxDll.Text.Value);

                var alreadyAssignedWorkItemsListEvents = ListEvents<WorkItemInfo>.Create("AlreadyAssignedWorkItems");
                alreadyAssignedWorkItemsListEvents.AddRange(alreadyAssignedWorkItems);
                
                var alteadyAssignedTreeGridConfiguration = new TreeConfiguration([
                    (Resources.TestCaseIdLabel, new BeUnitSize(BeSizingType.Fit)),
                    (Resources.TestCaseTitleLabel, new BeUnitSize(BeSizingType.Fill)),
                ]);
                    
                var alreadyAssignedTreeGrid = alreadyAssignedWorkItemsListEvents.GetBeTree(lt, (_, element, _, _) => new List<BeControl>()
                {
                    BeControls.BeLabel(element.Id.ToString()),
                    BeControls.BeLabel(element.Name)
                },_ => ListEvents<WorkItemInfo>.Create("Children"),  alteadyAssignedTreeGridConfiguration);

                #endregion
                
                grid.AddElement(BeControls.BeLabel(Resources.AlreadyAssignedTC));
                grid.AddElement(alreadyAssignedTreeGrid);
                
                grid.AddElement(BeControls.GetAutoGrid()
                    .AddElement(BeControls.BeLabel(Resources.TestCaseAzureId)));

                #region AssignButton

                grid.AddElement(
                    BeControls.GetAutoGrid(orientation: GridOrientation.Horizontal)
                        .AddElement( BeControls.GetAutoGrid()
                                .AddElement(textBoxTcId),
                            BeSizingType.Fill)
                        .AddElement(BeControls.GetAutoGrid()
                            .AddElement(BeControls.GetButton(Resources.AssingButton, lt, () =>
                            {
                                try
                                {
                                    var workItem = AzureDevOpsOperations.GetWorkItem(new WorkItemRequestBaseData(selectedToken)
                                    {
                                        TestPath = textBoxTestPath.Text.Value,
                                        ProjectDll = textBoxDll.Text.Value,
                                        TestCaseId = textBoxTcId.Text.Value
                                    });
                    
                                    if (workItem is not null)
                                    {
                                        var assign = MessageBox.ShowYesNo(
                                            Resources.ConfirmAssignmentForTestCase + $" {workItem.id}: {workItem.fields.SystemTitle}?", 
                                            Resources.AssignmentDialogTitle);
                    
                                        if (assign)
                                        {
                                            Submit(
                                                new WorkItemRequestBaseData(selectedToken)
                                                {
                                                    TestPath = textBoxTestPath.Text.Value,
                                                    ProjectDll = textBoxDll.Text.Value,
                                                    TestCaseId = textBoxTcId.Text.Value
                                                });
                                    
                                            var updatedAlreadyAssigned = AzureDevOpsOperations.GetAssignedWorkItems(
                                                new WorkItemRequestBaseData(selectedToken)
                                                {
                                                    TestPath = textBoxTestPath.Text.Value,
                                                    ProjectDll = textBoxDll.Text.Value,
                                                });
                    
                                            foreach (var item in updatedAlreadyAssigned)
                                            {
                                                if (alreadyAssignedWorkItems.All(s => s.Id != item.Id))
                                                    alreadyAssignedWorkItemsListEvents.Add(item);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.ShowError(Resources.Error, Resources.WorkitemNotFound);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.ShowError(Resources.Error, $"{Resources.CheckTokenData}. {Resources.Error}: {ex.Message}");
                                }
                            }))));

                #endregion
                
                return BeControls.GetDialog(
                        dialogContent: grid,
                        title: Resources.Title,
                        id: nameof(AssignTestToTestCaseAction))
                    .WithCustomFooter(
                        BeControls.GetAutoGrid()
                            .AddElement(BeControls.GetSpacer()
                                .WithLineBorder(BeShowBorders.OnlyTop, lt))
                            .AddElement(BeControls.BeLabel(Resources.SupportMyWork))
                            .AddElement(BeControls.GetLinkButton("https://buymeacoffee.com/krzysztof.rutana",
                                lt,
                                () =>
                                {
                                    clipboard.SetText("https://buymeacoffee.com/krzysztof.rutana");

                                    MessageBox.ShowInfo(Resources.LinkCopied);
                                })))
                    .WithCustomButton(Resources.Close, lt, dialogButtonAction: BeCommonBehavior.CLOSE);
            },
            parentLifetime: Lifetime.Eternal);
    }

    private IUnitTestTreeNode GetLastChild(IUnitTestTreeNode selectedNode)
    {
        if (selectedNode is null)
            return null;
        
        if (selectedNode.HasChildren)
            return GetLastChild(selectedNode.Children.First());

        return selectedNode;
    }

    private void Submit(WorkItemRequestBaseData requestBaseData)
    {
        if (!requestBaseData.Validate(false, out var errorMessage))
        {
            MessageBox.ShowError(errorMessage);
            return;
        }

        try
        {
            using (var httpClient = new HttpClient())
            {
                var workitem = AzureDevOpsOperations.GetWorkitem(httpClient, requestBaseData);

                if (!requestBaseData.CheckWorkItemMusBeUpdated(workitem, out var testcaseIdGuid))
                {
                    MessageBox.ShowInfo(Resources.TestAlreadyAssigned);
                    return;
                }
                
                var result = AzureDevOpsOperations.UpdateWorkitem(
                    httpClient, 
                    requestBaseData,
                    testcaseIdGuid);

                if(result.IsSuccessStatusCode)
                    MessageBox.ShowInfo(Resources.TestCaseUpdated);
                else
                {
                    var message = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    MessageBox.ShowError($"{Resources.Error}: {message}");
                }
            }
            
        }
        catch (Exception ex)
        {
            MessageBox.ShowError($"{Resources.Error}: {ex.Message}");
        }
    }
}