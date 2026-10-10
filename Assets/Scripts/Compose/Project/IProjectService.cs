using System;
using System.Collections.Generic;
using ArcCreate.Data;
using UnityEngine;

namespace ArcCreate.Compose.Project
{
    public interface IProjectService
    {
        /// <summary>
        /// Event invoked after a chart has been loaded.
        /// </summary>
        event Action<ChartSettings> OnChartLoad;

        /// <summary>
        /// Event invoked after a project has been loaded.
        /// </summary>
        event Action<ProjectSettings> OnProjectLoad;

        /// <summary>
        /// Event invoked after the Beyond/Inscribed display preset changes.
        /// </summary>
        event Action<ChartSettings> OnDifficultyPresetChange;

        /// <summary>
        /// Gets the current project settings.
        /// </summary>
        ProjectSettings CurrentProject { get; }

        /// <summary>
        /// Gets the current chart settings.
        /// </summary>
        ChartSettings CurrentChart { get; }

        /// <summary>
        /// Gets the default display colors for each difficulties.
        /// </summary>
        List<Color> DefaultDifficultyColors { get; }

        /// <summary>
        /// Create a new project and load it.
        /// This will copy all files to the new project's folder.
        /// </summary>
        /// <param name="info">Details of the new project.</param>
        void CreateNewProject(NewProjectInfo info);

        /// <summary>
        /// Create a new chart and add it to the current project.
        /// </summary>
        /// <param name="name">The name of the chart file.</param>
        /// <param name="useInscribedPreset">Whether to initialize the chart with the Inscribed display preset.</param>
        void CreateNewChart(string name, bool useInscribedPreset = false);

        /// <summary>
        /// Apply the Beyond or Inscribed display preset to an existing chart.
        /// </summary>
        /// <param name="chart">The existing chart using the shared difficulty file path.</param>
        /// <param name="useInscribedPreset">Whether to apply the Inscribed rather than Beyond.</param>
        void ApplyDifficultyPreset(ChartSettings chart, bool useInscribedPreset);

        /// <summary>
        /// Load a chart and invoke <see cref="OnChartLoad"/>.
        /// </summary>
        /// <param name="chart">The chart to load.</param>
        void OpenChart(ChartSettings chart);

        /// <summary>
        /// Remove a chart from the current project (without deleting the actual chart file on the file system).
        /// Removing the currently active chart is not allowed and will be ignored.
        /// </summary>
        /// <param name="chart">The chart to remove.</param>
        void RemoveChart(ChartSettings chart);

        /// <summary>
        /// Save the current project.
        /// </summary>
        void SaveProject();

        /// <summary>
        /// Load a project from a project file on disk.
        /// </summary>
        /// <param name="projectPath">Path to the project file.</param>
        void OpenProject(string projectPath);
    }
}
