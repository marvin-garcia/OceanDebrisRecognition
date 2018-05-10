using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Cognitive.CustomVision.Training;
using Microsoft.Cognitive.CustomVision.Training.Models;

namespace TrainCustomVisionApi
{
    class Program
    {
        // Custom Vision Training API key
        private static string CustomVisionTrainingApiKey = "";

        private static string projectName = "";

        private static TrainingApi trainingApi = new TrainingApi();

        private static string objectTagName = "Object";

        private static string oceanTagName = "Ocean";

        private static string objectTrainImagesPath = @"<your repo local path>\OceanDebrisRecognition\TrainCustomVisionApi\ImageSet\Train\Object";

        private static string noObjectTrainImagesPath = @"<your repo local path>\OceanDebrisRecognition\TrainCustomVisionApi\ImageSet\Train\Ocean";

        static void Main(string[] args)
        {
            bool result = MainAsync(args).ContinueWith((r) =>
            {
                return r.Result;
            }).Result;
        }

        private static async Task<bool> MainAsync(string[] args)
        {
            try
            {
                trainingApi.ApiKey = CustomVisionTrainingApiKey;

                // Create project
                var projects = await trainingApi.GetProjectsAsync();
                var project = projects.Where(x => x.Name == projectName).FirstOrDefault();

                if (project == null)
                {
                    Console.WriteLine($"\nCreating project '{projectName}'");
                    project = await trainingApi.CreateProjectAsync(projectName);
                }

                // Retrieve all tags
                var tagsList = await trainingApi.GetTagsAsync(project.Id);

                #region Create Object tag
                var objectTag = tagsList.Tags.Where(x => x.Name == objectTagName).FirstOrDefault();

                if (objectTag == null)
                {
                    Console.WriteLine($"\nCreating tag '{objectTagName}'");
                    objectTag = await trainingApi.CreateTagAsync(project.Id, objectTagName);
                }

                // add images to tag
                Console.WriteLine($"\nAdding images to tag '{objectTagName}'");
                List<ImageFileCreateEntry> imageFiles = (Directory.GetFiles(objectTrainImagesPath)).Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img))).ToList();
                await trainingApi.CreateImagesFromFilesAsync(project.Id, new ImageFileCreateBatch(imageFiles, new List<Guid>() { objectTag.Id }));
                #endregion

                #region Create Ocean tag
                var oceanTag = tagsList.Tags.Where(x => x.Name == oceanTagName).FirstOrDefault();

                if (oceanTag == null)
                {
                    Console.WriteLine($"\nCreating tag '{oceanTagName}'");
                    oceanTag = await trainingApi.CreateTagAsync(project.Id, oceanTagName);
                }

                // add images
                Console.WriteLine($"\nAdding images to tag '{oceanTagName}'");
                imageFiles = (Directory.GetFiles(noObjectTrainImagesPath)).Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img))).ToList();
                await trainingApi.CreateImagesFromFilesAsync(project.Id, new ImageFileCreateBatch(imageFiles, new List<Guid>() { oceanTag.Id }));
                #endregion

                #region Train the classifier
                Console.WriteLine("\nTraining");
                var iteration = await trainingApi.TrainProjectAsync(project.Id);

                do
                {
                    Thread.Sleep(1000);
                    iteration = await trainingApi.GetIterationAsync(project.Id, iteration.Id);
                }
                while (string.Equals("training", iteration.Status, StringComparison.OrdinalIgnoreCase));

                if (!string.Equals(iteration.Status, "completed", StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"An error occurred training the classifier. Iteration status is {iteration.Status}");

                iteration.IsDefault = true;
                await trainingApi.UpdateIterationAsync(project.Id, iteration.Id, iteration);
                #endregion

                Console.WriteLine("\nFinished. Press Enter to exit");
                Console.Read();

                return true;
            }
            catch (Exception e)
            {
                Console.Write(e);
                Console.WriteLine("\nPress Enter to exit");
                Console.Read();

                return false;
            }
        }
    }
}
