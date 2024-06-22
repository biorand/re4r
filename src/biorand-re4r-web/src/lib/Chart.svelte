<script lang="ts" context="module">
    export function createChart(
        seriesName: string,
        x: string[],
        y: number[],
        yFormatter?: (y: number) => string
    ) {
        return {
            chart: {
                type: 'area',
                toolbar: { show: false }
            },
            dataLabels: {
                enabled: false
            },
            series: [
                {
                    name: seriesName,
                    data: y
                }
            ],
            xaxis: {
                categories: x,
                axisTicks: {
                    show: true
                },
                labels: {
                    show: true,
                    style: {
                        fontFamily: 'Inter, sans-serif',
                        cssClass: 'text-xs font-normal fill-gray-500 dark:fill-gray-400'
                    }
                }
            },
            yaxis: {
                min: 0,
                labels: {
                    formatter: yFormatter,
                    style: {
                        fontFamily: 'Inter, sans-serif',
                        cssClass: 'text-xs font-normal fill-gray-500 dark:fill-gray-400'
                    }
                }
            }
        };
    }
</script>

<script lang="ts">
    import ApexCharts from 'apexcharts';

    let className = '';
    export { className as class };
    export let options: any = undefined;

    export const chart = (node: any, options: any) => {
        let myChart = new ApexCharts(node, options);
        myChart.render();

        return {
            update(options: any) {
                myChart.updateOptions(options);
            },
            destroy() {
                myChart.destroy();
            }
        };
    };
</script>

<div class={className} use:chart={options} />
