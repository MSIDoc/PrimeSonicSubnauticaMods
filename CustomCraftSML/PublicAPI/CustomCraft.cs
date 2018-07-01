﻿namespace CustomCraft.PublicAPI
{
    using System;
    using SMLHelper.V2.Crafting;
    using SMLHelper.V2.Handlers;
    using UnityEngine.Assertions;

    public static class CustomCraft
    {
        public static void AddRecipe(TechType craftedItem, TechData recipe, CraftingPath craftingPath)
        {
            AddRecipe(craftedItem, recipe, craftingPath.Scheme, craftingPath.Path);
        }

        public static void AddRecipe(TechType craftedItem, TechData recipe, CraftTree.Type craftTree, string path)
        {
            Assert.AreNotEqual(craftedItem.ToString(), ((int)craftedItem).ToString(), "This API in intended only for use with standard, non-modded TechTypes.");
            // Only modded enums use the int string as their ToString value

            //CraftTreeHandler.customNodes.Add(new CustomCraftNode(craftedItem, craftTree, path));
            CraftDataHandler.AddToBuildableList(craftedItem);
            CraftDataHandler.EditTechData(craftedItem, recipe);
            ModCraftTreeRoot craftTreeRoot = CraftTreeHandler.GetExistingTree(craftTree);

            var steps = path.Split(CraftingNode.Splitter);

            ModCraftTreeNode node = craftTreeRoot;
            foreach (var step in steps)
            {
                node = (node as ModCraftTreeLinkingNode).GetTabNode(step);
            }
            (node as ModCraftTreeLinkingNode).AddCraftingNode(craftedItem);
        }

        public static void ModifyRecipe(TechType craftedItem, TechData recipe)
        {
            Assert.AreNotEqual(craftedItem.ToString(), ((int)craftedItem).ToString(), "This API in intended only for use with standard, non-modded TechTypes.");
            // Only modded enums use the int string as their ToString value

            CraftDataHandler.EditTechData(craftedItem, recipe);
        }

        public static void ModifyItemSize(TechType inventoryItem, int width, int height)
        {
            Assert.AreNotEqual(inventoryItem.ToString(), ((int)inventoryItem).ToString(), "This API in intended only for use with standard, non-modded TechTypes.");

            Assert.IsTrue(width > 0 && height > 0, "Values must be positive and non-zero");
            Assert.IsTrue(width < 6 && height < 6, "Values must be smaller than six to fit");
            // Value chosen for what should be the standard inventory size

            CraftDataHandler.EditItemSize(inventoryItem, new Vector2int(width, height));
            
        }
    }
}
