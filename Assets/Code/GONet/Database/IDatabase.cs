/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

using System.Collections.Generic;

namespace GONet.Database
{
    public interface IDatabaseRow
    {
        long UID { get; set; }
    }

    public interface IDatabase
    {
        long NoUID { get; }

        /// <param name="shouldBypassUIDUpdate">only with this is false will the <paramref name="item"/> be returned with the <see cref="IDatabaseRow.UID"/> set for new inserts...this is passed as true for performance reasons when the UID is not needed after the save, because an addiitonal database read is performed when this value is false in order to retrieve the UID.</param>
        bool Save<T>(T item, bool shouldBypassUIDUpdate = false) where T : IDatabaseRow;

        /// <param name="shouldBypassUIDUpdate">only with this is false will the elements inside <paramref name="items"/> be returned with the <see cref="IDatabaseRow.UID"/> set for new inserts...this is passed as true for performance reasons when the UID is not needed after the save, because an addiitonal database read is performed when this value is false in order to retrieve the UID.</param>
        bool Save<T>(IEnumerable<T> items, int limit = int.MaxValue, bool shouldBypassUIDUpdate = true) where T : IDatabaseRow;

        T Read<T>(long uid) where T : IDatabaseRow;

        T ReadFirstOrDefault<T>(params TableQueryColumnFilter<T>[] queryFilters) where T : IDatabaseRow;

        List<T> ReadAll<T>() where T : IDatabaseRow;

        List<T> ReadList<T>(params TableQueryColumnFilter<T>[] queryFilters) where T : IDatabaseRow;

        bool Delete<T>(long uid) where T : IDatabaseRow;

        bool Delete(IDatabaseRow item);

        int Delete<T>(params TableQueryColumnFilter<T>[] queryFilters) where T : IDatabaseRow;
    }
}
