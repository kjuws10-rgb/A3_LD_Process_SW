using System.Globalization;
using System.IO;
using System.Windows;
using Drilling.UI.Popup;
using Drilling.Common.Managers;
using Drilling.Common.Interface;
using Drilling.Common.Motion;
using Drilling.Common.Alarm;
using Drilling.Common.InterLock;
using Drilling.Common.Station;
using System.Windows.Media;

namespace Drilling.UI.Menu.Menus;

public sealed class CMenuRecipe : IMenu
{
    private readonly IRecipeManager _recipeManager;
    private readonly Func<string> _selectedRecipeIdProvider;
    private readonly Action<string> _selectedRecipeIdSetter;
    private readonly Func<string> _selectedCategoryProvider;
    private readonly Action<string> _selectedCategorySetter;
    private string _selectedGroup = "ALL";
    private readonly Func<CMenuRecipe?> _editScreenProvider;
    private readonly Action<string> _setStatusMessage;
    private readonly Action<EN_MENU, string> _showLoadingScreen;
    private readonly Action _refreshShellStatus;
    private readonly Func<Task> _refreshCurrentScreen;

    public CMenuRecipe(
        IRecipeManager recipeManager,
        Func<string> selectedRecipeIdProvider,
        Action<string> selectedRecipeIdSetter,
        Func<string> selectedCategoryProvider,
        Action<string> selectedCategorySetter,
        Func<CMenuRecipe?> editScreenProvider,
        Action<string> setStatusMessage,
        Action<EN_MENU, string> showLoadingScreen,
        Action refreshShellStatus,
        Func<Task> refreshCurrentScreen)
    {
        _recipeManager = recipeManager;
        _selectedRecipeIdProvider = selectedRecipeIdProvider;
        _selectedRecipeIdSetter = selectedRecipeIdSetter;
        _selectedCategoryProvider = selectedCategoryProvider;
        _selectedCategorySetter = selectedCategorySetter;
        _editScreenProvider = editScreenProvider;
        _setStatusMessage = setStatusMessage;
        _showLoadingScreen = showLoadingScreen;
        _refreshShellStatus = refreshShellStatus;
        _refreshCurrentScreen = refreshCurrentScreen;

        SelectCommand = new CButtonCommand(async parameter => await Select(parameter));
        SelectCategoryCommand = new CButtonCommand(async parameter => await SelectCategory(parameter));
        SelectGroupCommand = new CButtonCommand(async parameter => await SelectGroup(parameter));
        CreateCommand = new CButtonCommand(async _ => await Create());
        ModifyCommand = new CButtonCommand(async _ => await Modify());
        SaveCommand = new CButtonCommand(async _ => await Save());
        DeleteCommand = new CButtonCommand(async _ => await Delete());
    }

    public EN_MENU Menu => EN_MENU.Recipe;

    public IReadOnlyList<ST_DISPLAY_ITEM> RecipeList { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> Parameters { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> History { get; private set; } = [];

    public IReadOnlyList<ST_DISPLAY_ITEM> Actions { get; private set; } = [];

    public string SelectedRecipeFile { get; private set; } = "";

    public IReadOnlyList<ST_RECIPE_FILE> RecipeFiles { get; private set; } = [];

    public IReadOnlyList<ST_RECIPE_CATEGORY_TAB> ItemTabs { get; private set; } = [];

    public IReadOnlyList<ST_RECIPE_GROUP_TAB> GroupTabs { get; private set; } = [];

    public string SelectedGroup { get; private set; } = "ALL";

    public IReadOnlyList<ST_RECIPE_MANAGED_ITEM> AllManagedItems { get; private set; } = [];

    public IReadOnlyList<ST_RECIPE_MANAGED_ITEM> ManagedItems { get; private set; } = [];

    public IReadOnlyList<ST_RECIPE_HISTORY_ROW> ChangeHistory { get; private set; } = [];

    public IReadOnlyList<ST_RECIPE_STATE_ROW> StateRows { get; private set; } = [];

    public CButtonCommand SelectCommand { get; }

    public CButtonCommand SelectCategoryCommand { get; }

    public CButtonCommand SelectGroupCommand { get; }

    public CButtonCommand CreateCommand { get; }

    public CButtonCommand ModifyCommand { get; }

    public CButtonCommand SaveCommand { get; }

    public CButtonCommand DeleteCommand { get; }

    public async Task<CScreenViewModel> Build(CancellationToken cancellationToken = default)
    {
        var recipes = await _recipeManager.LoadRecipes(cancellationToken);
        var recipe = GetSelectedRecipe(recipes, _selectedRecipeIdProvider());
        var selectedRecipeFile = GetRecipeFileName(recipe);
        var loadedManagedItems = BuildManagedItems(recipe);
        var allManagedItems = GetEditItems(loadedManagedItems, _editScreenProvider(), selectedRecipeFile);
        var categories = BuildCategories(allManagedItems);
        var selectedCategory = NormalizeCategory(_selectedCategoryProvider(), categories);
        var categoryFilteredItems = string.IsNullOrWhiteSpace(selectedCategory)
            ? []
            : allManagedItems.Where(item => item.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase)).ToArray();
        var groups = BuildGroups(categoryFilteredItems);
        var selectedGroup = NormalizeGroup(_selectedGroup, groups);
        var filteredManagedItems = selectedGroup == "ALL"
            ? categoryFilteredItems
            : categoryFilteredItems.Where(item => item.SourceGroup.Equals(selectedGroup, StringComparison.OrdinalIgnoreCase)).ToArray();
        Apply(
            recipes.Select(item => new ST_DISPLAY_ITEM(item.Id, item.Name)).ToArray(),
            recipe?.Parameters.Select(item =>
                new ST_DISPLAY_ITEM(item.Name, item.Value, $"{item.Unit} / {item.Range}")).ToArray() ?? [],
            recipe?.History.Select(item =>
                new ST_DISPLAY_ITEM(item.ChangedAt.ToString("yyyy-MM-dd HH:mm"), item.ItemName, $"{item.OldValue} -> {item.NewValue} / {item.OperatorId}")).ToArray() ?? [],
            BuildActions(),
            selectedRecipeFile,
            BuildRecipeFiles(recipes, recipe),
            BuildCategoryTabs(categories, selectedCategory),
            BuildGroupTabs(groups, selectedGroup),
            selectedGroup,
            allManagedItems,
            filteredManagedItems,
            BuildChangeHistory(recipe),
            BuildStateRows(recipe, selectedRecipeFile, allManagedItems));

        return new CScreenViewModel(
            EN_MENU.Recipe,
            "RECIPE / MANAGE",
            "Recipe item edit, create, modify, save, delete.",
            [
                new("Recipe Count", recipes.Count.ToString()),
                new("Selected", selectedRecipeFile)
            ],
            [
                new("Managed Items", recipe?.Parameters.Select(item =>
                    new ST_DISPLAY_ITEM(item.Name, $"{item.Value} {item.Unit}".Trim(), item.Range)).ToArray() ?? []),
                new("Change History", recipe?.History.Select(item =>
                new ST_DISPLAY_ITEM(item.ItemName, $"{item.OldValue} -> {item.NewValue}", item.OperatorId)).ToArray() ?? [])
            ],
            recipe: this);
    }

    private async Task Select(object? parameter)
    {
        var recipeId = GetRecipeIdFromParameter(parameter);

        if (string.IsNullOrWhiteSpace(recipeId))
        {
            return;
        }

        _selectedRecipeIdSetter(recipeId);
        NotifyCommands();
        _setStatusMessage($"Recipe {recipeId}.csv selected.");
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private async Task SelectCategory(object? parameter)
    {
        if (parameter is not string category || string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        var selectedCategory = category.Trim().ToUpperInvariant();
        _selectedCategorySetter(selectedCategory);
        _selectedGroup = "ALL";
        _setStatusMessage($"Recipe category {selectedCategory} selected.");
        await _refreshCurrentScreen();
    }

    private async Task SelectGroup(object? parameter)
    {
        if (parameter is not string group || string.IsNullOrWhiteSpace(group))
        {
            return;
        }

        _selectedGroup = group.Trim().ToUpperInvariant();
        _setStatusMessage($"Recipe group {_selectedGroup} selected.");
        await _refreshCurrentScreen();
    }

    private async Task Save()
    {
        var recipeId = Path.GetFileNameWithoutExtension(SelectedRecipeFile);

        if (string.IsNullOrWhiteSpace(recipeId))
        {
            _setStatusMessage("Recipe save skipped. No recipe is selected.");
            return;
        }

        var recipeParameters = AllManagedItems
            .Select(item => CreateRecipeParameterFromRow(item, recipeId))
            .ToArray();
        var validationMessage = ValidateRecipeParameters(recipeParameters);

        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            _setStatusMessage(validationMessage);
            return;
        }

        var recipeName = GetEditedRecipeName(recipeParameters, recipeId);

        await _recipeManager.SaveRecipe(new ST_RECIPE_DATA(recipeId, recipeName, recipeParameters, []));

        NotifyCommands();
        _setStatusMessage($"Recipe {recipeId}.csv saved and CSV verified.");
        _showLoadingScreen(EN_MENU.Recipe, "RECIPE");
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private async Task Modify()
    {
        var oldRecipeId = GetRecipeIdFromParameter(SelectedRecipeFile);

        if (string.IsNullOrWhiteSpace(oldRecipeId))
        {
            _setStatusMessage("Recipe rename skipped. No recipe is selected.");
            return;
        }

        var recipes = await _recipeManager.LoadRecipes();
        var newRecipeId = ShowRecipeNameDialog(
            "Modify Recipe Name",
            "Enter the new recipe name.",
            oldRecipeId,
            value => ValidateRecipeId(NormalizeRecipeIdInput(value), recipes, oldRecipeId));

        if (newRecipeId is null)
        {
            _setStatusMessage("Recipe rename canceled.");
            return;
        }

        if (newRecipeId.Equals(oldRecipeId, StringComparison.OrdinalIgnoreCase))
        {
            _setStatusMessage("Recipe rename skipped. Name was not changed.");
            return;
        }

        var validationMessage = ValidateRecipeId(newRecipeId, recipes, oldRecipeId);

        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            _setStatusMessage(validationMessage);
            return;
        }

        var recipeParameters = AllManagedItems
            .Select(item => CreateRecipeParameterFromRow(item, oldRecipeId))
            .ToArray();
        validationMessage = ValidateRecipeParameters(recipeParameters);

        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            _setStatusMessage(validationMessage);
            return;
        }

        if (AllManagedItems.Any(item => item.IsEdited))
        {
            await _recipeManager.SaveRecipe(new ST_RECIPE_DATA(oldRecipeId, oldRecipeId, recipeParameters, []));
        }

        await _recipeManager.RenameRecipe(oldRecipeId, newRecipeId);

        _selectedRecipeIdSetter(newRecipeId);
        _selectedCategorySetter("ALL");
        _selectedGroup = "ALL";
        NotifyCommands();
        _setStatusMessage($"Recipe {oldRecipeId}.csv renamed to {newRecipeId}.csv and CSV verified.");
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private async Task Create()
    {
        if (AllManagedItems.Count == 0)
        {
            _setStatusMessage("Recipe create skipped. No source recipe is loaded.");
            return;
        }

        var recipes = await _recipeManager.LoadRecipes();
        var recipeId = ShowRecipeNameDialog(
            "Create Recipe",
            "Enter the new recipe name.",
            "",
            value => ValidateRecipeId(NormalizeRecipeIdInput(value), recipes));

        if (recipeId is null)
        {
            _setStatusMessage("Recipe create canceled.");
            return;
        }

        var recipeNameValidationMessage = ValidateRecipeId(recipeId, recipes);

        if (!string.IsNullOrWhiteSpace(recipeNameValidationMessage))
        {
            _setStatusMessage(recipeNameValidationMessage);
            return;
        }

        var recipeParameters = AllManagedItems
            .Select(item => CreateRecipeParameterFromRow(item, recipeId))
            .ToArray();
        var validationMessage = ValidateRecipeParameters(recipeParameters);

        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            _setStatusMessage(validationMessage);
            return;
        }

        await _recipeManager.SaveRecipe(new ST_RECIPE_DATA(recipeId, recipeId, recipeParameters, []));

        _selectedRecipeIdSetter(recipeId);
        _selectedCategorySetter("ALL");
        _selectedGroup = "ALL";
        NotifyCommands();
        _setStatusMessage($"Recipe {recipeId}.csv created from current recipe and CSV verified.");
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private async Task Delete()
    {
        var recipeId = GetRecipeIdFromParameter(SelectedRecipeFile);

        if (string.IsNullOrWhiteSpace(recipeId))
        {
            _setStatusMessage("Recipe delete skipped. No recipe is selected.");
            return;
        }

        if (!ConfirmRecipeDelete(recipeId))
        {
            _setStatusMessage($"Recipe {recipeId}.csv delete canceled.");
            return;
        }

        await _recipeManager.DeleteRecipe(recipeId);

        _selectedRecipeIdSetter("");
        _selectedCategorySetter("ALL");
        _selectedGroup = "ALL";
        NotifyCommands();
        _setStatusMessage($"Recipe {recipeId}.csv deleted.");
        _refreshShellStatus();
        await _refreshCurrentScreen();
    }

    private void NotifyCommands()
    {
        SaveCommand.NotifyCanExecuteChanged();
        ModifyCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    private void Apply(
        IReadOnlyList<ST_DISPLAY_ITEM> recipeList,
        IReadOnlyList<ST_DISPLAY_ITEM> parameters,
        IReadOnlyList<ST_DISPLAY_ITEM> history,
        IReadOnlyList<ST_DISPLAY_ITEM> actions,
        string selectedRecipeFile,
        IReadOnlyList<ST_RECIPE_FILE> recipeFiles,
        IReadOnlyList<ST_RECIPE_CATEGORY_TAB> itemTabs,
        IReadOnlyList<ST_RECIPE_GROUP_TAB> groupTabs,
        string selectedGroup,
        IReadOnlyList<ST_RECIPE_MANAGED_ITEM> allManagedItems,
        IReadOnlyList<ST_RECIPE_MANAGED_ITEM> managedItems,
        IReadOnlyList<ST_RECIPE_HISTORY_ROW> changeHistory,
        IReadOnlyList<ST_RECIPE_STATE_ROW> stateRows)
    {
        RecipeList = recipeList;
        Parameters = parameters;
        History = history;
        Actions = actions;
        SelectedRecipeFile = selectedRecipeFile;
        RecipeFiles = recipeFiles;
        ItemTabs = itemTabs;
        GroupTabs = groupTabs;
        SelectedGroup = selectedGroup;
        AllManagedItems = allManagedItems;
        ManagedItems = managedItems;
        ChangeHistory = changeHistory;
        StateRows = stateRows;
        NotifyCommands();
    }

    private static ST_RECIPE_DATA? GetSelectedRecipe(
        IReadOnlyList<ST_RECIPE_DATA> recipes,
        string selectedRecipeId)
    {
        if (!string.IsNullOrWhiteSpace(selectedRecipeId))
        {
            var selectedRecipe = recipes.FirstOrDefault(recipe =>
                recipe.Id.Equals(selectedRecipeId, StringComparison.OrdinalIgnoreCase));

            if (selectedRecipe is not null)
            {
                return selectedRecipe;
            }
        }

        return recipes.FirstOrDefault();
    }

    private static IReadOnlyList<ST_RECIPE_MANAGED_ITEM> GetEditItems(
        IReadOnlyList<ST_RECIPE_MANAGED_ITEM> loadedItems,
        CMenuRecipe? editScreen,
        string selectedRecipeFile)
    {
        return editScreen is not null &&
            editScreen.SelectedRecipeFile.Equals(selectedRecipeFile, StringComparison.OrdinalIgnoreCase) &&
            editScreen.AllManagedItems.Count > 0
                ? editScreen.AllManagedItems
                : loadedItems;
    }

    private static IReadOnlyList<ST_DISPLAY_ITEM> BuildActions()
    {
        return
        [
            new("Create", "Ready"),
            new("Save", "Ready"),
            new("Delete", "Ready"),
            new("Modify", "Rename")
        ];
    }

    private static string GetRecipeFileName(ST_RECIPE_DATA? recipe)
    {
        return recipe is null ? "" : $"{recipe.Id}.csv";
    }

    private static IReadOnlyList<ST_RECIPE_FILE> BuildRecipeFiles(
        IReadOnlyList<ST_RECIPE_DATA> recipes,
        ST_RECIPE_DATA? selectedRecipe)
    {
        return recipes
            .Select((recipe, index) => new ST_RECIPE_FILE(
                (index + 1).ToString("00"),
                GetRecipeFileName(recipe),
                selectedRecipe is not null && recipe.Id.Equals(selectedRecipe.Id, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static IReadOnlyList<ST_RECIPE_MANAGED_ITEM> BuildManagedItems(ST_RECIPE_DATA? recipe)
    {
        if (recipe is null)
        {
            return [];
        }

        return recipe.Parameters
            .Where(parameter => parameter.Use && parameter.Show)
            .Select((parameter, index) => new { Parameter = parameter, Index = index })
            .OrderBy(item => item.Parameter.DisplayOrder <= 0 ? int.MaxValue : item.Parameter.DisplayOrder)
            .ThenBy(item => item.Index)
            .Select(item =>
            {
                var parameter = item.Parameter;
                var category = NormalizeRecipeText(parameter.Tab, "COMMON");
                var group = NormalizeRecipeText(parameter.Group, category);

                return new ST_RECIPE_MANAGED_ITEM(
                    category,
                    group,
                    parameter.Name,
                    parameter.Value,
                    NormalizeUnit(parameter.Unit),
                    parameter.Description,
                    GetValueState(parameter),
                    parameter.Key,
                    group,
                    parameter.DataType,
                    parameter.ChangeLimit,
                    parameter.Min,
                    parameter.Max);
            })
            .ToArray();
    }

    private static IReadOnlyList<string> BuildCategories(IReadOnlyList<ST_RECIPE_MANAGED_ITEM> managedItems)
    {
        return managedItems
            .Select(item => item.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<ST_RECIPE_CATEGORY_TAB> BuildCategoryTabs(
        IReadOnlyList<string> categories,
        string selectedCategory)
    {
        return categories
            .Select(category => new ST_RECIPE_CATEGORY_TAB(
                category,
                category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static IReadOnlyList<string> BuildGroups(IReadOnlyList<ST_RECIPE_MANAGED_ITEM> managedItems)
    {
        var groups = managedItems
            .Select(item => item.SourceGroup)
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new[] { "ALL" }.Concat(groups).ToArray();
    }

    private static IReadOnlyList<ST_RECIPE_GROUP_TAB> BuildGroupTabs(
        IReadOnlyList<string> groups,
        string selectedGroup)
    {
        return groups
            .Select(group => new ST_RECIPE_GROUP_TAB(
                group,
                group.Equals(selectedGroup, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static IReadOnlyList<ST_RECIPE_HISTORY_ROW> BuildChangeHistory(ST_RECIPE_DATA? recipe)
    {
        return recipe?.History.Select(item => new ST_RECIPE_HISTORY_ROW(
            item.ChangedAt.ToString("HH:mm:ss"),
            string.IsNullOrWhiteSpace(item.Action) ? item.OperatorId : item.Action,
            item.Tab,
            item.Group,
            item.ItemName,
            item.OldValue,
            item.NewValue)).ToArray() ?? [];
    }

    private static IReadOnlyList<ST_RECIPE_STATE_ROW> BuildStateRows(
        ST_RECIPE_DATA? recipe,
        string selectedRecipeFile,
        IReadOnlyList<ST_RECIPE_MANAGED_ITEM> managedItems)
    {
        if (recipe is null)
        {
            return
            [
                new("Modified Items", "0"),
                new("Recipe File", "-"),
                new("Edit State", "No Recipe")
            ];
        }

        var modifiedCount = managedItems.Count(item => item.IsEdited);

        return
        [
            new("Modified Items", modifiedCount.ToString(), modifiedCount > 0 ? "Warn" : "Ok"),
            new("Recipe File", selectedRecipeFile, "Accent"),
            new("Edit State", modifiedCount > 0 ? "Modified" : "Loaded", modifiedCount > 0 ? "Warn" : "Ok")
        ];
    }

    private static string NormalizeCategory(string category, IReadOnlyList<string> categories)
    {
        if (categories.Count == 0)
        {
            return "";
        }

        var normalized = NormalizeRecipeText(category, categories[0]);
        return categories.Any(item => item.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ? normalized
            : categories[0];
    }

    private static string NormalizeGroup(string group, IReadOnlyList<string> groups)
    {
        var normalized = NormalizeRecipeText(group, "ALL");
        return groups.Any(item => item.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ? normalized
            : "ALL";
    }

    private static string NormalizeRecipeText(string value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim().ToUpperInvariant();
    }

    private static string NormalizeUnit(string unit)
    {
        return string.IsNullOrWhiteSpace(unit) ? "-" : unit;
    }

    private static string GetValueState(ST_RECIPE_PARAM parameter)
    {
        if (IsModified(parameter))
        {
            return "Warn";
        }

        return parameter.Key.Equals("RECIPE_NAME", StringComparison.OrdinalIgnoreCase) ||
            parameter.Key.Equals("SCANNER_COUNT", StringComparison.OrdinalIgnoreCase) ||
            IsOnOffValue(parameter.Value)
                ? "Accent"
                : "Normal";
    }

    private static bool IsModified(ST_RECIPE_PARAM parameter)
    {
        return !string.Equals(
            NormalizeRecipeValue(parameter.Value),
            NormalizeRecipeValue(parameter.DefaultValue),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOnOffValue(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized is "ON" or "OFF";
    }

    private static string NormalizeRecipeValue(string value)
    {
        return value.Trim();
    }

    private static string GetRecipeIdFromParameter(object? parameter)
    {
        var value = parameter switch
        {
            ST_RECIPE_FILE recipeFile => recipeFile.FileName,
            string text => text,
            _ => ""
        };

        return Path.GetFileNameWithoutExtension(value.Trim());
    }

    private static ST_RECIPE_PARAM CreateRecipeParameterFromRow(
        ST_RECIPE_MANAGED_ITEM item,
        string recipeId)
    {
        var value = item.Key.Equals("RECIPE_NAME", StringComparison.OrdinalIgnoreCase)
            ? recipeId
            : item.Value;

        return new ST_RECIPE_PARAM(
            item.Item,
            value,
            NormalizeRecipeUnit(item.Unit),
            "",
            item.OriginalValue,
            item.Category,
            item.SourceGroup,
            item.Key,
            item.Description,
            true,
            true,
            0,
            item.DataType,
            item.ChangeLimit,
            item.Min,
            item.Max);
    }

    private static string? ShowRecipeNameDialog(
        string title,
        string message,
        string initialValue,
        Func<string, string>? validate = null)
    {
        var dialog = new CRecipeNameDialog(title, message, initialValue, validate)
        {
            Owner = GetActiveWindow()
        };

        return dialog.ShowDialog() == true
            ? NormalizeRecipeIdInput(dialog.RecipeName)
            : null;
    }

    private static bool ConfirmRecipeDelete(string recipeId)
    {
        var dialog = new CRecipeConfirmDialog(
            "Delete Recipe",
            $"Delete {recipeId}.csv?\nThis operation removes the recipe file from the RECIPE folder.",
            "DELETE")
        {
            Owner = GetActiveWindow()
        };

        return dialog.ShowDialog() == true;
    }

    private static Window? GetActiveWindow()
    {
        return Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive);
    }

    private static string NormalizeRecipeIdInput(string value)
    {
        var normalized = value.Trim();

        if (normalized.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4];
        }

        return normalized.Trim();
    }

    private static string ValidateRecipeId(
        string recipeId,
        IReadOnlyList<ST_RECIPE_DATA> recipes,
        string currentRecipeId = "")
    {
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            return "Recipe name is required.";
        }

        foreach (var character in recipeId)
        {
            if (Path.GetInvalidFileNameChars().Contains(character))
            {
                return $"Recipe name cannot contain '{character}'.";
            }
        }

        if (recipeId is "." or ".." || recipeId.EndsWith(".", StringComparison.Ordinal))
        {
            return "Recipe name is not valid as a file name.";
        }

        var reservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        if (reservedNames.Contains(recipeId))
        {
            return "Recipe name is reserved by Windows.";
        }

        var exists = recipes.Any(recipe =>
            recipe.Id.Equals(recipeId, StringComparison.OrdinalIgnoreCase) &&
            !recipe.Id.Equals(currentRecipeId, StringComparison.OrdinalIgnoreCase));

        return exists
            ? $"Recipe {recipeId}.csv already exists."
            : "";
    }

    private static string ValidateRecipeParameters(IReadOnlyList<ST_RECIPE_PARAM> parameters)
    {
        foreach (var parameter in parameters)
        {
            var value = parameter.Value.Trim();

            if (parameter.Key.Equals("RECIPE_NAME", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(value))
            {
                return "Recipe save blocked. Recipe Name cannot be empty.";
            }

            var validationMessage = parameter.DataType switch
            {
                EN_RECIPE_DATA_TYPE.Int => ValidateIntParameter(parameter, value),
                EN_RECIPE_DATA_TYPE.Double => ValidateDoubleParameter(parameter, value),
                EN_RECIPE_DATA_TYPE.Bool => ValidateBoolParameter(parameter, value),
                _ => ""
            };

            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return validationMessage;
            }
        }

        return "";
    }

    private static string ValidateIntParameter(ST_RECIPE_PARAM parameter, string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return $"Recipe save blocked. {parameter.Name} must be an integer.";
        }

        return ValidateNumericRange(parameter, parsed);
    }

    private static string ValidateDoubleParameter(ST_RECIPE_PARAM parameter, string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return $"Recipe save blocked. {parameter.Name} must be numeric.";
        }

        return ValidateNumericRange(parameter, parsed);
    }

    private static string ValidateBoolParameter(ST_RECIPE_PARAM parameter, string value)
    {
        var normalized = value.Trim().ToUpperInvariant();

        return normalized is "ON" or "OFF" or "TRUE" or "FALSE" or "1" or "0" or "YES" or "NO"
            ? ""
            : $"Recipe save blocked. {parameter.Name} must be ON/OFF or TRUE/FALSE.";
    }

    private static string ValidateNumericRange(ST_RECIPE_PARAM parameter, double value)
    {
        if (!parameter.Min.Equals(parameter.Max) &&
            (value < parameter.Min || value > parameter.Max))
        {
            return $"Recipe save blocked. {parameter.Name} must be between {parameter.Min:0.###} and {parameter.Max:0.###}.";
        }

        if (parameter.ChangeLimit > 0 &&
            double.TryParse(parameter.DefaultValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var oldValue) &&
            Math.Abs(value - oldValue) > parameter.ChangeLimit)
        {
            return $"Recipe save blocked. {parameter.Name} change limit is +/-{parameter.ChangeLimit:0.###}.";
        }

        return "";
    }

    private static string GetEditedRecipeName(
        IReadOnlyList<ST_RECIPE_PARAM> parameters,
        string fallbackRecipeId)
    {
        return parameters.FirstOrDefault(item =>
                item.Key.Equals("RECIPE_NAME", StringComparison.OrdinalIgnoreCase) ||
                item.Name.Equals("Recipe Name", StringComparison.OrdinalIgnoreCase))?.Value
            ?? fallbackRecipeId;
    }

    private static string NormalizeRecipeUnit(string unit)
    {
        return unit == "-" ? "" : unit;
    }
}

public sealed record ST_RECIPE_CATEGORY_TAB(
    string Category,
    bool IsSelected);

public sealed record ST_RECIPE_GROUP_TAB(
    string Group,
    bool IsSelected);

public sealed record ST_RECIPE_FILE(
    string No,
    string FileName,
    bool IsSelected);

public sealed class ST_RECIPE_MANAGED_ITEM : CBindingBase
{
    private readonly string _initialValue;
    private readonly string _initialValueState;
    private string _value;
    private string _valueState;

    public ST_RECIPE_MANAGED_ITEM(
        string category,
        string group,
        string item,
        string value,
        string unit,
        string description,
        string valueState = "Normal",
        string key = "",
        string sourceGroup = "",
        EN_RECIPE_DATA_TYPE dataType = EN_RECIPE_DATA_TYPE.String,
        double changeLimit = 0.0,
        double min = 0.0,
        double max = 0.0)
    {
        Category = category;
        Group = group;
        Item = item;
        Unit = unit;
        Description = description;
        Key = key;
        SourceGroup = sourceGroup;
        DataType = dataType;
        ChangeLimit = changeLimit;
        Min = min;
        Max = max;
        _value = value;
        _valueState = valueState;
        _initialValue = value;
        _initialValueState = valueState;
    }

    public string Category { get; }

    public string Group { get; }

    public string Item { get; }

    public string Value
    {
        get => _value;
        set
        {
            if (!SetProperty(ref _value, value))
            {
                return;
            }

            ValueState = IsEdited ? "Warn" : _initialValueState;
            OnPropertyChanged(nameof(IsEdited));
        }
    }

    public string Unit { get; }

    public string Description { get; }

    public string Key { get; }

    public string SourceGroup { get; }

    public string OriginalValue => _initialValue;

    public EN_RECIPE_DATA_TYPE DataType { get; }

    public double ChangeLimit { get; }

    public double Min { get; }

    public double Max { get; }

    public bool IsEdited => !NormalizeValue(Value).Equals(NormalizeValue(_initialValue), StringComparison.OrdinalIgnoreCase);

    public string ValueState
    {
        get => _valueState;
        private set
        {
            if (SetProperty(ref _valueState, value))
            {
                OnPropertyChanged(nameof(ValueBrush));
            }
        }
    }

    public Brush ValueBrush => ValueState switch
    {
        "Accent" => CStatusBrush.Simul,
        "Warn" => CStatusBrush.Wait,
        _ => CStatusBrush.PrimaryText
    };

    private static string NormalizeValue(string value)
    {
        return value.Trim();
    }
}

public sealed record ST_RECIPE_HISTORY_ROW(
    string Time,
    string Action,
    string Tab,
    string Group,
    string Item,
    string Before,
    string After)
{
    public Brush AfterBrush => CStatusBrush.Wait;
}

public sealed record ST_RECIPE_STATE_ROW(
    string Name,
    string Value,
    string State = "Normal")
{
    public Brush ValueBrush => State switch
    {
        "Accent" => CStatusBrush.Simul,
        "Warn" => CStatusBrush.Wait,
        "Ok" => CStatusBrush.Online,
        _ => CStatusBrush.PrimaryText
    };
}




