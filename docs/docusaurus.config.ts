import { themes as prismThemes } from 'prism-react-renderer';
import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'WaaS',
  tagline: 'A language-independent scripting engine for Unity and .NET using WebAssembly.',
  favicon: 'img/favicon.ico',
  url: 'https://ruccho.com/',
  baseUrl: '/WaaS/',
  organizationName: 'ruccho',
  projectName: 'WaaS',
  trailingSlash: false,

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  i18n: {
    defaultLocale: 'en',
    locales: ['en', 'ja'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl:
            'https://github.com/ruccho/WaaS/tree/main/',
          routeBasePath: '/'
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/social.png',
    navbar: {
      title: 'WaaS',
      logo: {
        alt: 'WaaS Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'localeDropdown',
          position: 'left'
        },
        {
          href: 'https://github.com/ruccho/WaaS',
          position: 'right',
          className: 'header-github-link',
        },
      ],
      hideOnScroll: true
    },
    footer: {
      style: 'light',
      copyright: `© ${new Date().getFullYear()} ruccho`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'bash'],
    },
    colorMode: {
      defaultMode: 'dark',
    },
  } satisfies Preset.ThemeConfig,
  markdown: {
    mermaid: true,
  },
  themes: ['@docusaurus/theme-mermaid'],
};

export default config;