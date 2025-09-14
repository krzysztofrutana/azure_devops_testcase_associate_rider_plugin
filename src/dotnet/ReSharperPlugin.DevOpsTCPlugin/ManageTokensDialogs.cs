using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.UI.Icons.CommonThemedIcons;
using JetBrains.DataFlow;
using JetBrains.IDE.UI;
using JetBrains.IDE.UI.Extensions;
using JetBrains.IDE.UI.Extensions.Properties;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rider.Model.UIAutomation;
using JetBrains.Util;
using ReSharperPlugin.DevOpsTCPlugin.Icons;
using ReSharperPlugin.DevOpsTCPlugin.Models;
using ReSharperPlugin.DevOpsTCPlugin.Settings;

namespace ReSharperPlugin.DevOpsTCPlugin;

public static class ManageTokensDialogs
{
    public static BeButton GetManageTokensDialog(ISolution solution, Lifetime lt, DevOpsSettingsStore settingsStore, DevOpsSettings settings, IconsProvider iconHost, IDialogHost dialogHost)
    {
        return BeControls.GetOpenDialogButton(lt, Resources.ManageTokensTitle, () =>
        {
            var conf = new TreeConfiguration([
                (Resources.Organization, new BeUnitSize(BeSizingType.Fill)),
                (Resources.ProjectInOrganization, new BeUnitSize(BeSizingType.Fill)),
                (Resources.Solution, new BeUnitSize(BeSizingType.Fill)),
                (Resources.PatLabel, new BeUnitSize(BeSizingType.Fill))
            ]);

            var elementsInfo = new Dictionary<DevOpsToken, TokenTreeGridInfo>();
            
            var treeGrid = settings.TokensListEvent.GetBeTree(lt, (_, element, properties, _) =>
            {
                var controls = new List<BeControl>()
                {
                    BeControls.BeLabel(element.Organization),
                    BeControls.BeLabel(element.OrganizationProject),
                    BeControls.BeLabel(element.SolutionName),
                    BeControls.BeLabel(Helpers.HidePat(element.Pat))
                };
                
                if (!elementsInfo.TryGetValue(element, out var _))
                {
                    elementsInfo.Add(element, new TokenTreeGridInfo
                    {
                        Token = element,
                        Properties = properties,
                        Controls = controls
                    });
                }

                return controls;
            },_ => ListEvents<DevOpsToken>.Create("Children"),  conf);
                
            var content = treeGrid.InToolbar()
                .AddItem(GetAddActionButton(solution, lt, iconHost, dialogHost, settingsStore, settings))
                .AddItem(GetEditActionButton(lt, iconHost, dialogHost, settingsStore, settings, elementsInfo))
                .AddItem(GetDeleteActionButton(lt, iconHost, dialogHost, settingsStore, settings, elementsInfo));
                    
            return BeControls.GetDialog(
                    dialogContent: content,
                    title: Resources.ManageTokensTitle,
                    id: nameof(GetManageTokensDialog), 
                    layoutPersistenceMode: DialogLayoutPersistenceMode.POSITION)
                .WithOkButton(lt, text: Resources.Close);
        }, dialogHost);
    }
    
    private static BeButton GetAddActionButton(ISolution solution, 
        Lifetime lt, 
        IconsProvider iconHost, 
        IDialogHost dialogHost, 
        DevOpsSettingsStore settingsStore,
        DevOpsSettings settings)
    {
        var createIcon = iconHost.GetIcon(CommonThemedIcons.Create.Id);
        var createActionDialog = BeControls.GetOpenDialogButton(lt, Resources.Add, () =>
        {
            BeTextBox textBoxOrganization = BeControls.GetTextBox(lt);
            BeTextBox textBoxProject = BeControls.GetTextBox(lt);
            BeTextBox textBoxSolution = BeControls.GetTextBox(lt, initialText: solution.Name);
            BeTextBox textBoxPat = BeControls.GetTextBox(lt);
            
            var grid = BeControls.GetAutoGrid();
            
            grid.AddElement(BeControls.BeLabel(Resources.Organization));
            grid.AddElement(textBoxOrganization);
            grid.AddElement(BeControls.BeLabel(Resources.ProjectInOrganization));
            grid.AddElement(textBoxProject);
            grid.AddElement(BeControls.BeLabel(Resources.Solution));
            grid.AddElement(textBoxSolution);
            grid.AddElement(BeControls.BeLabel(Resources.PatLabel));
            grid.AddElement(textBoxPat);
            
            return BeControls.GetDialog(
                    dialogContent: grid,
                    title: Resources.Add,
                    id: nameof(GetAddActionButton))
                .WithOkButton(lt, text: Resources.Save, ok: () =>
                {
                    var token = new DevOpsToken()
                    {
                        Pat = textBoxPat.Text.Value,
                        Organization = textBoxOrganization.Text.Value,
                        OrganizationProject = textBoxProject.Text.Value,
                        SolutionName = textBoxSolution.Text.Value
                    };

                    if (token.SomethingMissing)
                    {
                        throw new Exception(Resources.AllDataForTokenIsRequired);
                    }

                    settings.TokensListEvent.Add(token);
                    
                    settingsStore.SetSettings(settings);
                             
                    MessageBox.ShowInfo(Resources.SettingsSaved, Resources.OrganizationSettingsLabel);
                })
                .WithCancelButton(lt);
                
        }, dialogHost, createIcon, BeButtonStyle.ICON);

        return createActionDialog;
    }
    
    private static BeButton GetEditActionButton(Lifetime lt, 
        IconsProvider iconHost, 
        IDialogHost dialogHost, 
        DevOpsSettingsStore settingsStore,
        DevOpsSettings settings, 
        Dictionary<DevOpsToken, TokenTreeGridInfo> elementsInfo)
    {
        var editIcon = iconHost.GetIcon(CommonThemedIcons.Edit.Id);
        var editActionDialog = BeControls.GetOpenDialogButton(lt, Resources.Edit, () =>
        {
            var anySelected = elementsInfo.Any(x => x.Value.Properties.Selected.Value);
            if (!anySelected)
            {
                return GetNoTokenSelectedDialog(lt);
            }
            
            var selectedItem = elementsInfo.FirstOrDefault(x => x.Value.Properties.Selected.Value);
            
            var textBoxOrganization = BeControls.GetTextBox(lt, initialText: selectedItem.Key.Organization);
            var textBoxProject = BeControls.GetTextBox(lt, initialText: selectedItem.Key.OrganizationProject);
            var textBoxSolution = BeControls.GetTextBox(lt, initialText: selectedItem.Key.SolutionName);
            var textBoxPat = BeControls.GetTextBox(lt, initialText: selectedItem.Key.Pat);
            
            var grid = BeControls.GetAutoGrid();
            
            grid.AddElement(BeControls.BeLabel(Resources.Organization));
            grid.AddElement(textBoxOrganization);
            grid.AddElement(BeControls.BeLabel(Resources.ProjectInOrganization));
            grid.AddElement(textBoxProject);
            grid.AddElement(BeControls.BeLabel(Resources.Solution));
            grid.AddElement(textBoxSolution);
            grid.AddElement(BeControls.BeLabel(Resources.PatLabel));
            grid.AddElement(textBoxPat);
            
            return BeControls.GetDialog(
                    dialogContent: grid,
                    title: Resources.Edit,
                    id: nameof(GetEditActionButton))
                .WithOkButton(lt, text: Resources.Save, ok: () =>
                {
                    selectedItem.Key.Organization = textBoxOrganization.Text.Value;
                    var organizationLabel = selectedItem.Value.Controls[0] as BeLabel;
                    if(organizationLabel != null)
                        organizationLabel.Text.Value = textBoxOrganization.Text.Value;
                    
                    selectedItem.Key.OrganizationProject = textBoxProject.Text.Value;
                    var organizationProjectLabel = selectedItem.Value.Controls[1] as BeLabel;
                    if(organizationProjectLabel != null)
                        organizationProjectLabel.Text.Value = textBoxProject.Text.Value;
                    
                    selectedItem.Key.SolutionName = textBoxSolution.Text.Value;
                    var solutionNameLabel = selectedItem.Value.Controls[2] as BeLabel;
                    if(solutionNameLabel != null)
                        solutionNameLabel.Text.Value = textBoxSolution.Text.Value;
                    
                    selectedItem.Key.Pat = textBoxPat.Text.Value;
                    var patLabel = selectedItem.Value.Controls[3] as BeLabel;
                    if(patLabel != null)
                        patLabel.Text.Value = Helpers.HidePat(textBoxPat.Text.Value);

                    if (selectedItem.Key.SomethingMissing)
                    {
                        throw new Exception(Resources.AllDataForTokenIsRequired);
                    }

                    settingsStore.SetSettings(settings);
                             
                    MessageBox.ShowInfo(Resources.SettingsSaved, Resources.OrganizationSettingsLabel);
                })
                .WithCancelButton(lt);
                
        }, dialogHost, editIcon, BeButtonStyle.ICON);

        return editActionDialog;
    }
    
    private static BeButton GetDeleteActionButton(Lifetime lt, 
        IconsProvider iconHost, 
        IDialogHost dialogHost, 
        DevOpsSettingsStore settingsStore,
        DevOpsSettings settings,
        Dictionary<DevOpsToken, TokenTreeGridInfo> elementsInfo)
    {
        var deleteIcon = iconHost.GetIcon(CommonThemedIcons.Remove.Id);
        var deleteActionDialog = BeControls.GetOpenDialogButton(lt, Resources.Delete, () =>
        {
            var anySelected = elementsInfo.Any(x => x.Value.Properties.Selected.Value);

            if (!anySelected)
            {
                return GetNoTokenSelectedDialog(lt);
            }
            
            var selectedItem = elementsInfo.FirstOrDefault(x => x.Value.Properties.Selected.Value);
            
            BeLabel label = BeControls.BeLabel($"{Resources.DeleteTokenConfirmationMessage} {selectedItem.Key}");

            return BeControls.GetDialog(
                    dialogContent: label,
                    title: Resources.Delete,
                    id: nameof(GetDeleteActionButton),
                    style: BeDialogStyle.MESSAGE_BOX)
                .WithOkButton(lt, text: Resources.Delete, ok: () =>
                {
                    settings.TokensListEvent.Remove(selectedItem.Key);

                    settingsStore.SetSettings(settings);

                    MessageBox.ShowInfo(Resources.SettingsSaved, Resources.OrganizationSettingsLabel);
                })
                .WithCancelButton(lt);
        }, dialogHost, deleteIcon, BeButtonStyle.ICON);

        return deleteActionDialog;
    }

    private static BeDialog GetNoTokenSelectedDialog(Lifetime lt)
    {
        return BeControls.GetDialog(
                dialogContent: BeControls.BeLabel(Resources.NoTokenSelected),
                title: Resources.Error,
                id: nameof(GetNoTokenSelectedDialog),
                style: BeDialogStyle.MESSAGE_BOX)
            .WithCancelButton(lt);
    }
}