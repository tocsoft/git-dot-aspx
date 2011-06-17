namespace GitAspx.Lib {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using GitSharp.Core;
	using GitSharp.Core.Transport;

	public class Repository {
		private DirectoryInfo directory;

		public static Repository Open(DirectoryInfo directory) {
			if (GitSharp.Repository.IsValid(directory.FullName)) {
				return new Repository(directory);
			}

			return null;
		}

		public Repository(DirectoryInfo directory) {
			this.directory = directory;
		}

		public void AdvertiseUploadPack(Stream output) {
			using (var repository = GetRepository()) {
				var pack = new UploadPack(repository);
				pack.sendAdvertisedRefs(new RefAdvertiser.PacketLineOutRefAdvertiser(new PacketLineOut(output)));
			}
		}

		public void AdvertiseReceivePack(Stream output) {
			using (var repository = GetRepository()) {
				var pack = new ReceivePack(repository);
				pack.SendAdvertisedRefs(new RefAdvertiser.PacketLineOutRefAdvertiser(new PacketLineOut(output)));
			}
		}

		public void Receive(Stream inputStream, Stream outputStream) {
			using (var repository = GetRepository()) {
				var pack = new ReceivePack(repository);
				pack.setBiDirectionalPipe(false);
				pack.receive(inputStream, outputStream, outputStream);
			}
		}

		public void Upload(Stream inputStream, Stream outputStream) {
			using (var repository = GetRepository()) {
				using (var pack = new UploadPack(repository)) {
					pack.setBiDirectionalPipe(false);
					pack.Upload(inputStream, outputStream, outputStream);
				}
			}
		}



        public FileTree GetFileTree(string path)
        {
            var p = path ?? "";
            var pa = p.Split('/', '\\');

            FileTree file = null;
            using (var repository = new GitSharp.Repository(FullPath))
            {
                file = new FileTree(repository.Head.CurrentCommit.Tree);
            }
            
            foreach(var d in pa)
            {
                if (file.Contains(d))
                    file = file[d];
            }

            return file;
        }

        public CommitInfo GetLatestCommit()
        {
            using (var repository = new GitSharp.Repository(FullPath))
            {
                var commit = repository.Head.CurrentCommit;

                if (commit == null)
                {
                    return null;
                }

                return new CommitInfo
                {
                    Message = commit.Message,
                    Date = commit.CommitDate.DateTime
                };
            }
        }

		private GitSharp.Core.Repository GetRepository() {
			return GitSharp.Core.Repository.Open(directory);
		}

		public string Name {
			get { return directory.Name; }
		}

		public string FullPath {
			get { return directory.FullName; }
		}

		public string GitDirectory() {
			if(FullPath.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) {
				return FullPath;
			}

			return Path.Combine(FullPath, ".git");
		}

		public void UpdateServerInfo() {
			using (var rep = GetRepository()) {
				if (rep.ObjectDatabase is ObjectDirectory) {
					RefWriter rw = new SimpleRefWriter(rep, rep.getAllRefs().Values);
					rw.writePackedRefs();
					rw.writeInfoRefs();

					var packs = GetPackRefs(rep);
					WriteInfoPacks(packs, rep);
				}
			}
		}

		private void WriteInfoPacks(IEnumerable<string> packs, GitSharp.Core.Repository repository) {

			var w = new StringBuilder();

			foreach (string pack in packs) {
				w.Append("P ");
				w.Append(pack);
				w.Append('\n');
			}

			var infoPacksPath = Path.Combine(repository.ObjectsDirectory.FullName, "info/packs");
			var encoded = Encoding.ASCII.GetBytes(w.ToString());


			using (Stream fs = File.Create(infoPacksPath)) {
				fs.Write(encoded, 0, encoded.Length);
			}
		}

		private IEnumerable<string> GetPackRefs(GitSharp.Core.Repository repository) {
			var packDir = repository.ObjectsDirectory.GetDirectories().SingleOrDefault(x => x.Name == "pack");

			if(packDir == null) {
				return Enumerable.Empty<string>();
			}

			return packDir.GetFiles("*.pack").Select(x => x.Name).ToList();
		}
	}

    public class CommitInfo
    {
        public string Message { get; set; }
        public DateTime Date { get; set; }
    }

    public class FileTree : System.Collections.ObjectModel.KeyedCollection<string, FileTree>
    {
        public FileTree(GitSharp.Tree tree)
        {
            Name = tree.Name;
            Path = tree.Path;

            foreach (var n in tree.Children.Where(x => x is GitSharp.Tree).Cast<GitSharp.Tree>())
            {
                this.Add(new FileTree(n) { Parent = this });
            } 
            foreach (var n in tree.Children.Where(x => x is GitSharp.Leaf).Cast<GitSharp.Leaf>())
            {
                this.Add(new FileTree(n) { Parent = this });
            }
            IsDirectory = true;
        }

        public FileTree(GitSharp.Leaf leaf)
        {
            Name = leaf.Name;
            Path = leaf.Path;
            Extension = System.IO.Path.GetExtension(Name).TrimStart('.');
            

            IsDirectory = false;
            _leaf = leaf;
        }
        internal GitSharp.Leaf _leaf;

        public string Name { get; private set; }
        public string Extension { get; private set; }

        public string Path { get; private set; }
        public FileTree Parent { get; private set; }
        public bool IsDirectory { get; set; }
        

        public byte[] Data { get { return _leaf.RawData; } }
        public string TextData { get { return _leaf.Data; } }

        public override string ToString()
        {
            return Name;
        }
        protected override string GetKeyForItem(FileTree item)
        {
            return item.Name;
        }

        public IEnumerable<FileTree> Parents() { 
            var p = this.Parent;
            while (p != null)
            {
                yield return p;
                p = p.Parent;
            }
        }
    }

}