const path = require('path');
const webpack = require('webpack');
const UglifyJsPlugin = require('uglifyjs-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const OptimizeCSSAssetsPlugin = require("optimize-css-assets-webpack-plugin");

module.exports = argv => {
    // Configuration in common to both client-side and server-side bundles
    const isDevBuild = argv && argv.mode === 'development';
    const bundleOutputDir = './wwwroot/dist';
    return [{
        stats: { modules: false },
        context: __dirname,
        resolve: { extensions: ['.js', '.ts'] },
        entry: { 'main-client': './ClientApp/boot.ts' },
        output: {
            path: path.join(__dirname, bundleOutputDir),
            filename: '[name].js',
            publicPath: 'dist/' // Webpack dev middleware, if enabled, handles requests for this URL prefix
        },
        module: {
            rules: [
                { test: /\.ts$/, include: /ClientApp/, use: 'awesome-typescript-loader' },
                {
                    test: /\.s?[ac]ss$/,
                    use: [{
                        loader: MiniCssExtractPlugin.loader,
                    }, {
                        loader: 'css-loader',
                        options: { sourceMap: isDevBuild }
                    }, {
                        loader: 'postcss-loader',
                        options: {
                            plugins: function () {
                                return [
                                    require('precss'),
                                    require('autoprefixer')
                                ];
                            },
                            sourceMap: isDevBuild
                        }
                    }, {
                        loader: "resolve-url-loader",
                        options: { sourceMap: isDevBuild }
                    }, {
                        loader: "sass-loader",
                        options: { sourceMap: isDevBuild }
                    }]
                }, {
                    test: /\.(png|jpg|jpeg|gif|svg|eot|ttf|woff|woff2)$/,
                    loader: 'url-loader',
                    options: {
                        limit: 25000,
                        name: '[path][name].[ext]'
                    }
                }
            ]
        },
        mode: isDevBuild ? 'development' : 'production',
        plugins: [
            new webpack.DllReferencePlugin({
                context: __dirname,
                manifest: require('./wwwroot/dist/vendor-manifest.json')
            }),
            new webpack.ProvidePlugin({
                $: 'jquery',
                jQuery: 'jquery',
            }),
            new MiniCssExtractPlugin({
                filename: '[name].css',
                chunkFilename: '[id].css'
            })
        ],
        devtool: 'source-map',
        optimization: {
            minimizer: [
                new UglifyJsPlugin({
                    cache: true,
                    parallel: true,
                    sourceMap: true
                }),
                new OptimizeCSSAssetsPlugin({})
            ]
        }
    }];
};
