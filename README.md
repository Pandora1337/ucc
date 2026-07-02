<a id="readme-top"></a>


<div align="center">

  <!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
  <!-- Shields.io badges. You can a comprehensive list with many more badges at: https://github.com/inttter/md-badges -->
  [![Issues](https://img.shields.io/github/issues/Pandora1337/ucc.svg?style=for-the-badge)](https://github.com/Pandora1337/ucc/issues)
  [![Release](https://img.shields.io/github/v/release/Pandora1337/ucc?style=for-the-badge)](https://github.com/Pandora1337/ucc/releases/latest)
  [![License](https://img.shields.io/github/license/Pandora1337/ucc.svg?style=for-the-badge)](https://github.com/Pandora1337/ucc/blob/main/LICENSE)

  [![.NET 8](https://img.shields.io/badge/.NET_8-%23512bd4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com)
  [![Blazor](https://img.shields.io/badge/Blazor-%23512bd4?style=for-the-badge&logo=blazor)](https://dotnet.microsoft.com/aspnet/blazor)
  [![Bootstrap](https://img.shields.io/badge/Bootstrap-563D7C?style=for-the-badge&logo=bootstrap&logoColor=white)](https://getbootstrap.com)
  [![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=fff)](https://hub.docker.com/)
</div>

<!-- PROJECT LOGO -->
<br />

<p align="center">
  <a href="https://github.com/Pandora1337/ucc">
    <img src="https://github.com/Pandora1337/ucc/raw/main/wwwroot/favicon.png" alt="Logo" width="64" height="64">
  </a>
</p>

<h3 align="center">Universal Crafting Calculator</h3>

<p align="center">
  Fully client-side, feature-rich, and user configurable crafting calculator for any game. Define items, add them to recipes, and calculate the resources needed, all without editing files or creating accounts!
  <br />
  <br />
  <a href="https://pandora1337.github.io/ucc">
    Check it out!
  </a>
  <br />
  <br />
  <a href="https://github.com/Pandora1337/ucc/issues/new?labels=bug&template=bug-report---.md">
    Report Bug
  </a>
  &middot;
  <a href="https://github.com/Pandora1337/ucc/issues/new?labels=enhancement&template=feature-request---.md">
    Request Feature
  </a>
</p>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#features">Features</a></li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li>
      <a href="#self-hosting">Self-Hosting</a>
      <ul>
        <li><a href="#docker">Docker</a></li>
        <li><a href="#static-site-hosting">Static Site Hosting</a></li>
      </ul>
    </li>
    <li>
      <a href="#development">Development</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#building">Building</a></li>
      </ul>
    </li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
  </ol>
</details>


### Features

- **Ease of use** - All editing happens inside the browser, no more editing files to add a new item or recipe!
- **Item Icons** - Supports user-added images for items
- **Offline-first** - No active internet connection required
- **Config Import / Export** - Save and share configs
- **Custom recipes** - Users define items and recipes themselves for any game of any version
- **Variants** - Supports many alternate recipes for the same item
- **Multi-Product** - A single recipe can make multiple items
- **Fractional Recipes** - Recipes support non-integer amounts of items, such as liquids
- **Mobile friendly** - all UI adapts dynamically to a phone or tablet screen
- **Darkmode** - Not a true website without it

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- USAGE EXAMPLES -->
## Usage

1. To use, visit http://pandora1337.github.io/ucc 
Or, if you want to run it yourself, see <a href="#self-hosting">Self-Hosting</a>.

2. To add items and recipes, go to the [Inventory](http://pandora1337.github.io/ucc/inventory) tab.

3. To calculate resource totals, go to the [Crafting](http://pandora1337.github.io/ucc/crafting) tab

<img alt="Preview-1" src="https://github.com/user-attachments/assets/50ed206b-badb-4b74-8c3b-df3e7d21f1f0" />
<img alt="Preview-2" src="https://github.com/user-attachments/assets/c2cba6a9-e3c9-480a-872b-4d6afe4b71d3" />


<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ROADMAP -->
### Roadmap

- [x] Storage usage stats
- [ ] Add Crafting Chance
- [ ] Item amount expression parsing
- [ ] Checklist page
- [ ] Sync Server

See the [open issues](https://github.com/Pandora1337/ucc/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#readme-top">back to top</a>)</p>



## Self-Hosting

### Docker

Using docker compose:

```yaml
services:
  ucc:
    image: pandora1337/ucc:latest
    container_name: ucc
    restart: unless-stopped
    ports:
      - 80:80 # CAN CHANGE : dont change
```

or docker run:

```bash
docker run --name ucc \
  -p 80:80 \ 
  --restart unless-stopped \
  pandora1337/ucc:latest
```

<p align="right">(<a href="#readme-top">back to top</a>)</p>



### Static Site hosting
1. Get the static website files from the [Latest Release](https://github.com/Pandora1337/ucc/releases/latest) or [Build them from source](#building-from-source).

    They are contained in `wwwroot` directory.
<br>

2. <span title="Single Page Application">`SPA`</span> Routing

    In BlazorWASM, routes for pages (such as `/inventory`)  don't have a corresponding `.html` file, resulting in a 404 error.

    To fix this, you need to redirect all uri page requests to `index.html`, which will depend on the webserver of your choice (see next steps for those), but a platform-agnostic solution would be to:

    i. Copy `index.html`
    ii. Rename `index.html` to `404.html`

<br>

3. NGINX Setup:

    You can use the `nginx.conf` from [here](https://github.com/Pandora1337/ucc/blob/main/nginx.conf).
    
    Or, if you want to use a custom NGINX config, make sure to include this:
    ```nginx
    server {
            location / {
                root /usr/share/nginx/html;
                try_files $uri $uri/ /index.html =404;
            }
        }
      ```
  <br>

4. For Apache, see this [guide](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/apache?view=aspnetcore-8.0).

<p align="right">(<a href="#readme-top">back to top</a>)</p>



## Development
### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [VSCode](https://code.visualstudio.com/) with [C# DevKit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extention, or Visual Studio.

### Building

1. Clone the repo
   ```sh
   git clone https://github.com/Pandora1337/ucc.git
   ```

2. Restore dependencies
   ```bash
   dotnet restore
   ```

3. Run locally in development mode
    ```bash
    dotnet run
    ```
    Open the URL shown in the console (typically `https://localhost:5001` or `http://localhost:5000`).


    If using VScode, there are launch shortcuts that can be accessed in `Run and Debug` menu. These create a webserver at `http://localhost:5153`

4. Enable hot reload (optional)

    This will reload the webserver when a file was changed
    ```bash
    dotnet watch run
    ```

5. Build static files for hosting
     ```bash
     dotnet publish -c Release
     ```

6. Host files in ```/bin/Release/net8.0/publish/wwwroot```.

    If deploying to Azure App Service or any other IIS, publish the contents of ```/bin/Release/net8.0/publish``` instead.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<p align="right">(<a href="#readme-top">back to top</a>)</p>


<!-- LICENSE -->
## License

Distributed under the GPL-3.0. See [`LICENSE`][license-url] for more information.



<!-- CONTACT -->
## Contact

Discord - [Pandora1337](https://discord.com/users/280299811310272513)


<p align="right">(<a href="#readme-top">back to top</a>)</p>
