﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms.Design;

namespace System.Windows.Forms.PropertyGridInternal
{
    internal partial class MultiSelectRootGridEntry
    {
        internal static class PropertyMerger
        {
            public static MultiPropertyDescriptorGridEntry[] GetMergedProperties(
                object[] objects,
                GridEntry parentEntry,
                PropertySort sort,
                PropertyTab tab)
            {
                MultiPropertyDescriptorGridEntry[] result = null;
                try
                {
                    int length = objects.Length;

                    if ((sort & PropertySort.Alphabetical) != 0)
                    {
                        ArrayList commonProperties = GetCommonProperties(objects, presort: true, tab, parentEntry);

                        var entries = new MultiPropertyDescriptorGridEntry[commonProperties.Count];
                        for (int i = 0; i < entries.Length; i++)
                        {
                            entries[i] = new MultiPropertyDescriptorGridEntry(
                                parentEntry.OwnerGrid,
                                parentEntry,
                                objects,
                                (PropertyDescriptor[])commonProperties[i],
                                hide: false);
                        }

                        result = SortParenEntries(entries);
                    }
                    else
                    {
                        object[] sortObjects = new object[length - 1];
                        Array.Copy(objects, 1, sortObjects, 0, length - 1);

                        ArrayList properties = GetCommonProperties(sortObjects, presort: true, tab, parentEntry);

                        // This will work for just one as well.
                        ArrayList firstProperties = GetCommonProperties(new object[] { objects[0] }, presort: false, tab, parentEntry);

                        var firstPropertyDescriptors = new PropertyDescriptor[firstProperties.Count];
                        for (int i = 0; i < firstProperties.Count; i++)
                        {
                            firstPropertyDescriptors[i] = ((PropertyDescriptor[])firstProperties[i])[0];
                        }

                        properties = UnsortedMerge(firstPropertyDescriptors, properties);

                        var entries = new MultiPropertyDescriptorGridEntry[properties.Count];

                        for (int i = 0; i < entries.Length; i++)
                        {
                            entries[i] = new MultiPropertyDescriptorGridEntry(
                                parentEntry.OwnerGrid,
                                parentEntry,
                                objects,
                                (PropertyDescriptor[])properties[i],
                                hide: false);
                        }

                        result = SortParenEntries(entries);
                    }
                }
                catch
                {
                }

                return result;
            }

            /// <summary>
            ///  Returns an array list of the PropertyDescriptor arrays, one for each component.
            /// </summary>
            private static ArrayList GetCommonProperties(object[] objects, bool presort, PropertyTab tab, GridEntry parentEntry)
            {
                var propertyCollections = new PropertyDescriptorCollection[objects.Length];
                var attributes = new Attribute[parentEntry.BrowsableAttributes.Count];

                parentEntry.BrowsableAttributes.CopyTo(attributes, 0);

                for (int i = 0; i < objects.Length; i++)
                {
                    var properties = tab.GetProperties(parentEntry, objects[i], attributes);
                    if (presort)
                    {
                        properties = properties.Sort(s_propertyComparer);
                    }

                    propertyCollections[i] = properties;
                }

                ArrayList mergedList = new();
                var matchArray = new PropertyDescriptor[objects.Length];

                //
                // Merge the property descriptors
                //

                int[] positions = new int[propertyCollections.Length];
                for (int i = 0; i < propertyCollections[0].Count; i++)
                {
                    PropertyDescriptor pivotProperty = propertyCollections[0][i];

                    bool match = pivotProperty.Attributes[typeof(MergablePropertyAttribute)].IsDefaultAttribute();

                    for (int j = 1; match && j < propertyCollections.Length; j++)
                    {
                        if (positions[j] >= propertyCollections[j].Count)
                        {
                            match = false;
                            break;
                        }

                        // Check to see if we're on a match.
                        PropertyDescriptor property = propertyCollections[j][positions[j]];
                        if (pivotProperty.Equals(property))
                        {
                            positions[j] += 1;

                            if (!property.Attributes[typeof(MergablePropertyAttribute)].IsDefaultAttribute())
                            {
                                match = false;
                                break;
                            }

                            matchArray[j] = property;
                            continue;
                        }

                        int position = positions[j];
                        property = propertyCollections[j][position];

                        match = false;

                        // If we aren't on a match, check all the items until we're past where the matching item would be.
                        while (s_propertyComparer.Compare(property, pivotProperty) <= 0)
                        {
                            // Got a match!
                            if (pivotProperty.Equals(property))
                            {
                                if (!property.Attributes[typeof(MergablePropertyAttribute)].IsDefaultAttribute())
                                {
                                    match = false;
                                    position++;
                                }
                                else
                                {
                                    match = true;
                                    matchArray[j] = property;
                                    positions[j] = position + 1;
                                }

                                break;
                            }

                            // Try again.
                            position++;
                            if (position < propertyCollections[j].Count)
                            {
                                property = propertyCollections[j][position];
                            }
                            else
                            {
                                break;
                            }
                        }

                        // If we got here, there is no match, quit for this one.
                        if (!match)
                        {
                            positions[j] = position;
                            break;
                        }
                    }

                    // Do we have a match?
                    if (match)
                    {
                        matchArray[0] = pivotProperty;
                        mergedList.Add(matchArray.Clone());
                    }
                }

                return mergedList;
            }

            private static MultiPropertyDescriptorGridEntry[] SortParenEntries(MultiPropertyDescriptorGridEntry[] entries)
            {
                MultiPropertyDescriptorGridEntry[] newEntries = null;
                int newPosition = 0;

                // First scan the list and move any parenthesized properties to the front.
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].ParensAroundName)
                    {
                        if (newEntries is null)
                        {
                            newEntries = new MultiPropertyDescriptorGridEntry[entries.Length];
                        }

                        newEntries[newPosition++] = entries[i];
                        entries[i] = null;
                    }
                }

                // Second pass, copy any that didn't have the parens.
                if (newPosition > 0)
                {
                    for (int i = 0; i < entries.Length; i++)
                    {
                        if (entries[i] is not null)
                        {
                            newEntries[newPosition++] = entries[i];
                        }
                    }

                    entries = newEntries;
                }

                return entries;
            }

            /// <summary>
            ///  Merges an unsorted array of grid entries with a sorted array of grid entries that
            ///  have already been merged. The resulting array is the intersection of entries between the two,
            ///  but in the order of <paramref name="baseEntries"/>.
            /// </summary>
            private static ArrayList UnsortedMerge(PropertyDescriptor[] baseEntries, ArrayList sortedMergedEntries)
            {
                ArrayList mergedEntries = new();
                var mergeArray = new PropertyDescriptor[((PropertyDescriptor[])sortedMergedEntries[0]).Length + 1];

                for (int i = 0; i < baseEntries.Length; i++)
                {
                    PropertyDescriptor basePd = baseEntries[i];

                    // First do a binary search for a matching item.
                    PropertyDescriptor[] mergedEntryList = null;
                    string entryName = $"{basePd.Name} {basePd.PropertyType.FullName}";

                    int length = sortedMergedEntries.Count;

                    // Perform a binary search.
                    int offset = length / 2;
                    int start = 0;

                    while (length > 0)
                    {
                        var propertyDescriptors = (PropertyDescriptor[])sortedMergedEntries[start + offset];
                        PropertyDescriptor propertyDescriptor = propertyDescriptors[0];
                        string sortString = $"{propertyDescriptor.Name} {propertyDescriptor.PropertyType.FullName}";
                        int result = string.Compare(entryName, sortString, ignoreCase: false, CultureInfo.InvariantCulture);
                        if (result == 0)
                        {
                            mergedEntryList = propertyDescriptors;
                            break;
                        }
                        else if (result < 0)
                        {
                            length = offset;
                        }
                        else
                        {
                            int delta = offset + 1;
                            start += delta;
                            length -= delta;
                        }

                        offset = length / 2;
                    }

                    if (mergedEntryList is not null)
                    {
                        mergeArray[0] = basePd;
                        Array.Copy(mergedEntryList, 0, mergeArray, 1, mergedEntryList.Length);
                        mergedEntries.Add(mergeArray.Clone());
                    }
                }

                return mergedEntries;
            }
        }
    }
}
